using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Publishes the primary domain's field textures and world extent as global shader properties so
    /// the water surface material can sample the field it displaces and shades. Runs for every camera:
    /// the values are persistent sim state, not this frame's graph output, so binding them is
    /// idempotent and cheap — only the sim step itself is gated to the game camera. It binds the
    /// last-completed buffers (matching the debug view) so a reader never races the in-flight compute.
    ///
    /// Texture globals are published through <c>SetGlobalTextureAfterPass</c>, which registers them in
    /// RenderGraph's global texture slots. URP's object passes call <c>UseAllGlobalTextures</c>, so this
    /// is the only kind of texture-global the transparent surface draw will actually bind — a raw
    /// command-buffer <c>SetGlobalTexture</c> is invisible to that mechanism and leaves the surface
    /// sampling an unbound (constant) texture. The scalar extent uniforms go through the command buffer.
    ///
    /// One publisher, one primary domain by design: a global shader property carries a single value, so
    /// it can only ever describe one water body. Multiple simultaneous bodies are a later migration to
    /// per-renderer property blocks, not something this global path can express.
    /// </summary>
    internal sealed class WaterFieldPublishPass : ScriptableRenderPass
    {
        internal WaterFieldPublishPass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var domain = WaterDomainRegistry.Primary;
            if (domain == null)
            {
                return;
            }

            var field = domain.Field;
            if (!field.Descriptor.Contains(WaterLayer.Dynamic)
                || !field.Descriptor.Contains(WaterLayer.SurfaceNormalFoam))
            {
                return;
            }

            // Import the last-completed buffers as graph resources so they can be assigned to global
            // texture slots. Dynamic's post-swap write target is untouched by this frame's compute, and
            // SurfaceNormalFoam is single-buffered, so both are safe to read this frame.
            var dynamicHandle = renderGraph.ImportTexture(field.WriteHandle(WaterLayer.Dynamic));
            var normalFoamHandle = renderGraph.ImportTexture(field.ReadHandle(WaterLayer.SurfaceNormalFoam));

            using var builder = renderGraph.AddUnsafePass<PassData>(
                WaterProfilingNames.FieldPublish, out var passData);

            FillExtent(passData, domain.Extent);

            builder.SetGlobalTextureAfterPass(dynamicHandle, WaterFieldShaderIds.DynamicGlobal);
            builder.SetGlobalTextureAfterPass(normalFoamHandle, WaterFieldShaderIds.NormalFoamGlobal);

            // Scalar globals go through the command buffer; this allowance permits that, keeps the pass
            // from being culled, and forces it to run before the passes that read the published globals.
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc<PassData>(PublishExtent);
        }

        private static void FillExtent(PassData passData, BoundedDomainExtent extent)
        {
            // Center is (world X, world Z); z/w are unused padding for the float4 uniform.
            passData.ExtentCenter = new Vector4(extent.Center.x, extent.Center.y, 0f, 0f);
            passData.ExtentSize = extent.SizeMeters;
            passData.ExtentTexelSize = extent.TexelSizeMeters;
        }

        private static void PublishExtent(PassData data, UnsafeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetGlobalVector(WaterFieldShaderIds.ExtentCenter, data.ExtentCenter);
            cmd.SetGlobalFloat(WaterFieldShaderIds.ExtentSize, data.ExtentSize);
            cmd.SetGlobalFloat(WaterFieldShaderIds.ExtentTexelSize, data.ExtentTexelSize);
        }

        private sealed class PassData
        {
            internal Vector4 ExtentCenter;
            internal float ExtentSize;
            internal float ExtentTexelSize;
        }
    }
}

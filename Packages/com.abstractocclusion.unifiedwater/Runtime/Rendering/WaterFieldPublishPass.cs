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
    /// One publisher, one primary domain by design: a global shader property carries a single value,
    /// so it can only ever describe one water body. Multiple simultaneous bodies are a later migration
    /// to per-renderer property blocks, not something this global path can express.
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

            PublishFieldTextures(field);
            PublishExtent(domain.Extent);
        }

        // Bind the last-completed buffers: Dynamic's post-swap write target is untouched by this
        // frame's compute, and SurfaceNormalFoam is single-buffered so its one texture is the result.
        private static void PublishFieldTextures(WaterField field)
        {
            Shader.SetGlobalTexture(WaterFieldShaderIds.DynamicGlobal, field.Write(WaterLayer.Dynamic));
            Shader.SetGlobalTexture(
                WaterFieldShaderIds.NormalFoamGlobal, field.Read(WaterLayer.SurfaceNormalFoam));
        }

        // Mirror BoundedDomainExtent's world footprint so the HLSL world->uv mapping stays a copy of
        // the C# formula. Center is (world X, world Z); z/w are unused padding for the float4 uniform.
        private static void PublishExtent(BoundedDomainExtent extent)
        {
            Shader.SetGlobalVector(
                WaterFieldShaderIds.ExtentCenter,
                new Vector4(extent.Center.x, extent.Center.y, UnusedUniformComponent, UnusedUniformComponent));
            Shader.SetGlobalFloat(WaterFieldShaderIds.ExtentSize, extent.SizeMeters);
            Shader.SetGlobalFloat(WaterFieldShaderIds.ExtentTexelSize, extent.TexelSizeMeters);
        }

        private const float UnusedUniformComponent = 0f;
    }
}

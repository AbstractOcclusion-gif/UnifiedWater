using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Derives the SurfaceNormalFoam layer from the Dynamic height field once per frame, after the
    /// ripple sim has finished its substeps. It reads the freshly integrated height and writes a
    /// surface normal, so consumers (the eventual surface shader, buoyancy) sample derived data and
    /// never raw sim state. Runs only on the last substep — the normal only needs the final height,
    /// not each intermediate one.
    /// </summary>
    internal sealed class WaveComposeProvider : IWaterFieldProvider
    {
        private static readonly WaterLayer[] NormalFoamOnly = { WaterLayer.SurfaceNormalFoam };

        private ComputeShader _compute;
        private int _kernel;
        private int _resolution;
        private int _cascadeCount;

        public IReadOnlyList<WaterLayer> WrittenLayers => NormalFoamOnly;

        public int WriteOrder => WaterProviderOrder.WaveCompose;

        public void Setup(WaterFieldDescriptor descriptor)
        {
            _compute = Resources.Load<ComputeShader>(WaterFieldShaderIds.ComposeComputeResourcePath);
            if (_compute == null)
            {
                throw new InvalidOperationException(
                    $"Compose compute shader not found at Resources path '{WaterFieldShaderIds.ComposeComputeResourcePath}'.");
            }

            _kernel = _compute.FindKernel(WaterFieldShaderIds.ComposeKernelName);
            _resolution = descriptor.Resolution;
            _cascadeCount = descriptor.CascadeCount;
        }

        public void RecordPasses(in WaterFieldFrame frame)
        {
            if (!frame.IsLastSubstep)
            {
                return;
            }

            // The Dynamic write handle holds the height the ripple sim just integrated this substep.
            var dynamic = frame.WriteHandle(WaterLayer.Dynamic);
            var normalFoam = frame.WriteHandle(WaterLayer.SurfaceNormalFoam);
            int groupsPerAxis = frame.Resolution / WaterFieldConstants.SimThreadGroupSize;

            using var builder = frame.RenderGraph.AddComputePass<PassData>(
                WaterProfilingNames.WaveCompose, out var passData);

            passData.Compute = _compute;
            passData.Kernel = _kernel;
            passData.Dynamic = dynamic;
            passData.NormalFoam = normalFoam;
            passData.GroupsPerAxis = groupsPerAxis;
            passData.Resolution = _resolution;
            passData.CascadeCount = _cascadeCount;

            builder.UseTexture(dynamic, AccessFlags.Read);
            builder.UseTexture(normalFoam, AccessFlags.Write);
            builder.SetRenderFunc<PassData>(Execute);
        }

        public void Dispose()
        {
            _compute = null;
        }

        private static void Execute(PassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.DynamicField, data.Dynamic);
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.NormalFoamField, data.NormalFoam);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.Resolution, data.Resolution);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.CascadeCount, data.CascadeCount);
            cmd.DispatchCompute(
                data.Compute, data.Kernel, data.GroupsPerAxis, data.GroupsPerAxis, data.CascadeCount);
        }

        private sealed class PassData
        {
            internal ComputeShader Compute;
            internal int Kernel;
            internal TextureHandle Dynamic;
            internal TextureHandle NormalFoam;
            internal int GroupsPerAxis;
            internal int Resolution;
            internal int CascadeCount;
        }
    }
}

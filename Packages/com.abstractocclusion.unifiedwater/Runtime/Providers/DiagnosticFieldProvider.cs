using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Temporary field provider that advances the Dynamic layer by a fixed increment each frame. It
    /// exists to prove the import → ping-pong → swap plumbing end to end before the real ripple
    /// simulation lands: it reads the previous frame's value and writes value + increment, so a
    /// correctly ping-ponged field climbs steadily — which the debug view shows and the play-mode
    /// test asserts. Replaced by the ripple provider in the next increment.
    /// </summary>
    internal sealed class DiagnosticFieldProvider : IWaterFieldProvider
    {
        private static readonly WaterLayer[] DynamicOnly = { WaterLayer.Dynamic };
        private const float PerFrameIncrement = 1f;

        private ComputeShader _compute;
        private int _kernel;
        private int _cascadeCount;

        public IReadOnlyList<WaterLayer> WrittenLayers => DynamicOnly;

        public int WriteOrder => WaterProviderOrder.Diagnostic;

        public void Setup(WaterFieldDescriptor descriptor)
        {
            _compute = Resources.Load<ComputeShader>(WaterFieldShaderIds.DiagnosticComputeResourcePath);
            if (_compute == null)
            {
                throw new InvalidOperationException(
                    "Diagnostic compute shader not found at Resources path " +
                    $"'{WaterFieldShaderIds.DiagnosticComputeResourcePath}'.");
            }

            _kernel = _compute.FindKernel(WaterFieldShaderIds.DiagnosticKernelName);
            _cascadeCount = descriptor.CascadeCount;
        }

        public void RecordPasses(in WaterFieldFrame frame)
        {
            var read = frame.ReadHandle(WaterLayer.Dynamic);
            var write = frame.WriteHandle(WaterLayer.Dynamic);
            int groupsPerAxis = frame.Resolution / WaterFieldConstants.SimThreadGroupSize;

            using var builder = frame.RenderGraph.AddComputePass<PassData>(
                WaterProfilingNames.DiagnosticAdvance, out var passData);

            passData.Compute = _compute;
            passData.Kernel = _kernel;
            passData.Read = read;
            passData.Write = write;
            passData.GroupsPerAxis = groupsPerAxis;
            passData.CascadeCount = _cascadeCount;

            builder.UseTexture(read, AccessFlags.Read);
            builder.UseTexture(write, AccessFlags.Write);
            builder.SetRenderFunc<PassData>(Execute);
        }

        public void Dispose()
        {
            // The compute shader is a shared Resources asset; drop the reference, do not destroy it.
            _compute = null;
        }

        private static void Execute(PassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.ReadField, data.Read);
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.WriteField, data.Write);
            cmd.SetComputeFloatParam(data.Compute, WaterFieldShaderIds.Increment, PerFrameIncrement);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.CascadeCount, data.CascadeCount);
            cmd.DispatchCompute(
                data.Compute, data.Kernel, data.GroupsPerAxis, data.GroupsPerAxis, data.CascadeCount);
        }

        private sealed class PassData
        {
            internal ComputeShader Compute;
            internal int Kernel;
            internal TextureHandle Read;
            internal TextureHandle Write;
            internal int GroupsPerAxis;
            internal int CascadeCount;
        }
    }
}

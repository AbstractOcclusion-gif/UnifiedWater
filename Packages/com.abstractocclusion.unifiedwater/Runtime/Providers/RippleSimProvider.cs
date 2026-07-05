using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Simulates the Dynamic layer as a height field. Each frame it integrates the wave equation
    /// (reads the previous state, writes the next) and then injects any queued impulses onto that
    /// result, so a drop appears and then spreads and damps over following frames. Height and
    /// velocity only — surface normals are derived downstream in the compose pass, keeping one
    /// implementation of that gradient. Tuning comes from the quality tier, never inline literals.
    /// </summary>
    internal sealed class RippleSimProvider : IWaterFieldProvider, IImpulseReceiver
    {
        private static readonly WaterLayer[] DynamicOnly = { WaterLayer.Dynamic };

        private readonly float _rippleDamping;
        private readonly float _propagationSpeed;
        private readonly List<WaterImpulse> _pendingImpulses = new List<WaterImpulse>();
        private readonly Vector4[] _impulseScratch = new Vector4[WaterSimConstants.MaxImpulsesPerFrame];

        private ComputeShader _compute;
        private int _integrateKernel;
        private int _injectKernel;
        private int _resolution;
        private int _cascadeCount;
        private ComputeBuffer _impulseBuffer;

        internal RippleSimProvider(WaterQualityTier tier)
        {
            if (tier == null)
            {
                throw new ArgumentNullException(nameof(tier));
            }

            _rippleDamping = tier.RippleDamping;
            _propagationSpeed = tier.PropagationSpeed;
        }

        public IReadOnlyList<WaterLayer> WrittenLayers => DynamicOnly;

        public int WriteOrder => WaterProviderOrder.RippleSim;

        public void Setup(WaterFieldDescriptor descriptor)
        {
            _compute = Resources.Load<ComputeShader>(WaterFieldShaderIds.RippleComputeResourcePath);
            if (_compute == null)
            {
                throw new InvalidOperationException(
                    $"Ripple compute shader not found at Resources path '{WaterFieldShaderIds.RippleComputeResourcePath}'.");
            }

            _integrateKernel = _compute.FindKernel(WaterFieldShaderIds.IntegrateKernelName);
            _injectKernel = _compute.FindKernel(WaterFieldShaderIds.InjectKernelName);
            _resolution = descriptor.Resolution;
            _cascadeCount = descriptor.CascadeCount;

            const int float4Stride = sizeof(float) * 4;
            _impulseBuffer = new ComputeBuffer(WaterSimConstants.MaxImpulsesPerFrame, float4Stride);
        }

        public void Enqueue(WaterImpulse impulse)
        {
            // Drop rather than grow past the fixed GPU buffer; a frame's worth of clicks never fills 64.
            if (_pendingImpulses.Count < WaterSimConstants.MaxImpulsesPerFrame)
            {
                _pendingImpulses.Add(impulse);
            }
        }

        public void RecordPasses(in WaterFieldFrame frame)
        {
            var read = frame.ReadHandle(WaterLayer.Dynamic);
            var write = frame.WriteHandle(WaterLayer.Dynamic);
            int groupsPerAxis = frame.Resolution / WaterFieldConstants.SimThreadGroupSize;

            RecordIntegrate(frame.RenderGraph, read, write, groupsPerAxis);
            RecordInject(frame.RenderGraph, write, groupsPerAxis);
        }

        public void Dispose()
        {
            _impulseBuffer?.Release();
            _impulseBuffer = null;
            _compute = null;
            _pendingImpulses.Clear();
        }

        private void RecordIntegrate(
            RenderGraph renderGraph, TextureHandle read, TextureHandle write, int groupsPerAxis)
        {
            using var builder = renderGraph.AddComputePass<IntegratePassData>(
                WaterProfilingNames.RippleIntegrate, out var passData);

            passData.Compute = _compute;
            passData.Kernel = _integrateKernel;
            passData.Read = read;
            passData.Write = write;
            passData.GroupsPerAxis = groupsPerAxis;
            passData.Resolution = _resolution;
            passData.CascadeCount = _cascadeCount;
            passData.Damping = _rippleDamping;
            passData.PropagationSpeed = _propagationSpeed;

            builder.UseTexture(read, AccessFlags.Read);
            builder.UseTexture(write, AccessFlags.Write);
            builder.SetRenderFunc<IntegratePassData>(ExecuteIntegrate);
        }

        private void RecordInject(RenderGraph renderGraph, TextureHandle write, int groupsPerAxis)
        {
            if (_pendingImpulses.Count == 0)
            {
                return;
            }

            int count = _pendingImpulses.Count;
            for (int i = 0; i < count; i++)
            {
                var impulse = _pendingImpulses[i];
                _impulseScratch[i] = new Vector4(
                    impulse.CenterUv.x, impulse.CenterUv.y, impulse.RadiusUv, impulse.Strength);
            }

            _impulseBuffer.SetData(_impulseScratch, 0, 0, count);
            _pendingImpulses.Clear();

            using var builder = renderGraph.AddComputePass<InjectPassData>(
                WaterProfilingNames.RippleInject, out var passData);

            passData.Compute = _compute;
            passData.Kernel = _injectKernel;
            passData.Write = write;
            passData.GroupsPerAxis = groupsPerAxis;
            passData.Resolution = _resolution;
            passData.CascadeCount = _cascadeCount;
            passData.Impulses = _impulseBuffer;
            passData.ImpulseCount = count;

            builder.UseTexture(write, AccessFlags.ReadWrite);
            builder.SetRenderFunc<InjectPassData>(ExecuteInject);
        }

        private static void ExecuteIntegrate(IntegratePassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.ReadField, data.Read);
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.WriteField, data.Write);
            cmd.SetComputeFloatParam(data.Compute, WaterFieldShaderIds.RippleDamping, data.Damping);
            cmd.SetComputeFloatParam(data.Compute, WaterFieldShaderIds.PropagationSpeed, data.PropagationSpeed);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.Resolution, data.Resolution);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.CascadeCount, data.CascadeCount);
            cmd.DispatchCompute(
                data.Compute, data.Kernel, data.GroupsPerAxis, data.GroupsPerAxis, data.CascadeCount);
        }

        private static void ExecuteInject(InjectPassData data, ComputeGraphContext context)
        {
            var cmd = context.cmd;
            cmd.SetComputeTextureParam(data.Compute, data.Kernel, WaterFieldShaderIds.WriteField, data.Write);
            cmd.SetComputeBufferParam(data.Compute, data.Kernel, WaterFieldShaderIds.Impulses, data.Impulses);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.ImpulseCount, data.ImpulseCount);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.Resolution, data.Resolution);
            cmd.SetComputeIntParam(data.Compute, WaterFieldShaderIds.CascadeCount, data.CascadeCount);
            cmd.DispatchCompute(
                data.Compute, data.Kernel, data.GroupsPerAxis, data.GroupsPerAxis, data.CascadeCount);
        }

        private sealed class IntegratePassData
        {
            internal ComputeShader Compute;
            internal int Kernel;
            internal TextureHandle Read;
            internal TextureHandle Write;
            internal int GroupsPerAxis;
            internal int Resolution;
            internal int CascadeCount;
            internal float Damping;
            internal float PropagationSpeed;
        }

        private sealed class InjectPassData
        {
            internal ComputeShader Compute;
            internal int Kernel;
            internal TextureHandle Write;
            internal int GroupsPerAxis;
            internal int Resolution;
            internal int CascadeCount;
            internal ComputeBuffer Impulses;
            internal int ImpulseCount;
        }
    }
}

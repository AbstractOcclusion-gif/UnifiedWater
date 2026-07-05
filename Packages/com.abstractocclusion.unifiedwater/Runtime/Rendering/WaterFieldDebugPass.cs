using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Blits the first registered domain's Dynamic layer to the screen as greyscale, for eyeballing
    /// that the field is advancing. Optional and toggled from the feature. It reads the layer's
    /// last-completed buffer (the post-swap write target, which this frame's compute does not touch),
    /// so the sampled texture is one frame old but race-free without coupling into the sim graph.
    /// </summary>
    internal sealed class WaterFieldDebugPass : ScriptableRenderPass, IDisposable
    {
        private Material _material;
        private float _valueScale;

        internal WaterFieldDebugPass()
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }

        internal void Configure(float valueScale)
        {
            _valueScale = valueScale;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            var domains = WaterDomainRegistry.Domains;
            if (domains.Count == 0)
            {
                return;
            }

            var field = domains[0].Field;
            if (!field.Descriptor.Contains(WaterLayer.Dynamic))
            {
                return;
            }

            EnsureMaterial();

            // Last-completed buffer: the current write target after the swap, not written this frame.
            _material.SetTexture(WaterFieldShaderIds.DebugField, field.Write(WaterLayer.Dynamic));
            _material.SetFloat(WaterFieldShaderIds.DebugValueScale, _valueScale);

            var resourceData = frameData.Get<UniversalResourceData>();

            using var builder = renderGraph.AddRasterRenderPass<PassData>(
                WaterProfilingNames.FieldDebugView, out var passData);
            passData.Material = _material;

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc<PassData>(Execute);
        }

        public void Dispose()
        {
            CoreUtils.Destroy(_material);
            _material = null;
        }

        private void EnsureMaterial()
        {
            if (_material != null)
            {
                return;
            }

            var shader = Shader.Find(WaterFieldShaderIds.DebugViewShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"Water field debug shader '{WaterFieldShaderIds.DebugViewShaderName}' not found.");
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
        }

        private static void Execute(PassData data, RasterGraphContext context)
        {
            CoreUtils.DrawFullScreen(context.cmd, data.Material, null, 0);
        }

        private sealed class PassData
        {
            internal Material Material;
        }
    }
}

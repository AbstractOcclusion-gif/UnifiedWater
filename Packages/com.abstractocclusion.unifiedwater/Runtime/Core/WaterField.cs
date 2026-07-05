using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Owns every GPU layer texture for one water domain. Allocates from an immutable
    /// <see cref="WaterFieldDescriptor"/> and releases on dispose. This is the shared spine
    /// all providers write and all consumers read; it holds storage only, never simulation
    /// logic, so a reader never has to jump elsewhere to know what the field contains.
    /// </summary>
    internal sealed class WaterField : IDisposable
    {
        private readonly Dictionary<WaterLayer, WaterLayerTarget> _targets;

        internal WaterFieldDescriptor Descriptor { get; }

        internal WaterField(WaterFieldDescriptor descriptor)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _targets = new Dictionary<WaterLayer, WaterLayerTarget>(descriptor.Layers.Count);
            Allocate();
        }

        internal RenderTexture Read(WaterLayer layer) => TargetOf(layer).Read;

        internal RenderTexture Write(WaterLayer layer) => TargetOf(layer).Write;

        internal RTHandle ReadHandle(WaterLayer layer) => TargetOf(layer).ReadHandle;

        internal RTHandle WriteHandle(WaterLayer layer) => TargetOf(layer).WriteHandle;

        internal void Swap(WaterLayer layer) => TargetOf(layer).Swap();

        public void Dispose()
        {
            foreach (var target in _targets.Values)
            {
                target.Dispose();
            }

            _targets.Clear();
        }

        private void Allocate()
        {
            var textureDescriptor = BuildTextureDescriptor();
            foreach (var layer in Descriptor.Layers)
            {
                textureDescriptor.graphicsFormat = WaterLayerCatalog.FormatOf(layer);
                _targets[layer] = new WaterLayerTarget(
                    layer.ToString(), textureDescriptor, WaterLayerCatalog.IsDoubleBuffered(layer));
            }
        }

        private RenderTextureDescriptor BuildTextureDescriptor()
        {
            return new RenderTextureDescriptor(Descriptor.Resolution, Descriptor.Resolution)
            {
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = Descriptor.CascadeCount,
                depthBufferBits = WaterFieldConstants.NoDepthBuffer,
                msaaSamples = WaterFieldConstants.NoMultiSampling,
                enableRandomWrite = true,
                useMipMap = false,
                sRGB = false
            };
        }

        private WaterLayerTarget TargetOf(WaterLayer layer)
        {
            if (_targets.TryGetValue(layer, out var target))
            {
                return target;
            }

            throw new ArgumentException($"Layer '{layer}' is not allocated in this field.", nameof(layer));
        }
    }
}

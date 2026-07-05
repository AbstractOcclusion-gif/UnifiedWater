using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// One frame's render-graph view of a domain's field, handed to each provider so it records its
    /// passes against layer handles the controller already imported. Importing every layer once, in
    /// the controller, means a shared-layer write-chain (ripple integrate then obstacle inject) sees
    /// one consistent read/write pair rather than importing the same persistent texture twice.
    /// </summary>
    internal readonly struct WaterFieldFrame
    {
        private readonly IReadOnlyDictionary<WaterLayer, LayerHandles> _handles;

        internal RenderGraph RenderGraph { get; }
        internal int Resolution { get; }
        internal int CascadeCount { get; }

        internal WaterFieldFrame(
            RenderGraph renderGraph,
            IReadOnlyDictionary<WaterLayer, LayerHandles> handles,
            int resolution,
            int cascadeCount)
        {
            RenderGraph = renderGraph;
            _handles = handles;
            Resolution = resolution;
            CascadeCount = cascadeCount;
        }

        internal TextureHandle ReadHandle(WaterLayer layer) => Resolve(layer).Read;

        internal TextureHandle WriteHandle(WaterLayer layer) => Resolve(layer).Write;

        private LayerHandles Resolve(WaterLayer layer)
        {
            if (_handles.TryGetValue(layer, out var handles))
            {
                return handles;
            }

            throw new ArgumentException(
                $"Layer '{layer}' is not imported this frame; only a domain's own layers are available.",
                nameof(layer));
        }

        /// <summary>The imported read and write handles for one layer. Equal for a single-buffered layer.</summary>
        internal readonly struct LayerHandles
        {
            internal TextureHandle Read { get; }
            internal TextureHandle Write { get; }

            internal LayerHandles(TextureHandle read, TextureHandle write)
            {
                Read = read;
                Write = write;
            }
        }
    }
}

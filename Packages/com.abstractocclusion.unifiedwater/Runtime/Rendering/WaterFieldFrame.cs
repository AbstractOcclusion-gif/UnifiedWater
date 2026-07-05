using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// One substep's render-graph view of a domain's field. The controller imports each layer's
    /// buffers once per frame; this view then presents the correct read and write handles for the
    /// current substep. For a double-buffered layer the two imported buffers swap roles each substep
    /// (parity on <see cref="SubstepIndex"/>), so a provider always reads the current state and writes
    /// the next without the field being re-imported mid-frame.
    /// </summary>
    internal readonly struct WaterFieldFrame
    {
        private readonly IReadOnlyDictionary<WaterLayer, BaseHandles> _handles;

        internal RenderGraph RenderGraph { get; }
        internal int Resolution { get; }
        internal int CascadeCount { get; }
        internal int SubstepIndex { get; }
        internal int SubstepCount { get; }

        internal bool IsFirstSubstep => SubstepIndex == 0;
        internal bool IsLastSubstep => SubstepIndex == SubstepCount - 1;

        internal WaterFieldFrame(
            RenderGraph renderGraph,
            IReadOnlyDictionary<WaterLayer, BaseHandles> handles,
            int resolution,
            int cascadeCount,
            int substepIndex,
            int substepCount)
        {
            RenderGraph = renderGraph;
            _handles = handles;
            Resolution = resolution;
            CascadeCount = cascadeCount;
            SubstepIndex = substepIndex;
            SubstepCount = substepCount;
        }

        internal TextureHandle ReadHandle(WaterLayer layer)
        {
            var handles = Resolve(layer);
            if (!handles.DoubleBuffered)
            {
                return handles.FrameStartRead;
            }

            return IsEvenSubstep ? handles.FrameStartRead : handles.FrameStartWrite;
        }

        internal TextureHandle WriteHandle(WaterLayer layer)
        {
            var handles = Resolve(layer);
            if (!handles.DoubleBuffered)
            {
                return handles.FrameStartRead;
            }

            return IsEvenSubstep ? handles.FrameStartWrite : handles.FrameStartRead;
        }

        private bool IsEvenSubstep => (SubstepIndex & 1) == 0;

        private BaseHandles Resolve(WaterLayer layer)
        {
            if (_handles.TryGetValue(layer, out var handles))
            {
                return handles;
            }

            throw new ArgumentException(
                $"Layer '{layer}' is not imported this frame; only a domain's own layers are available.",
                nameof(layer));
        }

        /// <summary>
        /// A layer's two imported buffers as they stood at the start of the frame. For a
        /// single-buffered layer both roles resolve to <see cref="FrameStartRead"/>.
        /// </summary>
        internal readonly struct BaseHandles
        {
            internal TextureHandle FrameStartRead { get; }
            internal TextureHandle FrameStartWrite { get; }
            internal bool DoubleBuffered { get; }

            internal BaseHandles(TextureHandle frameStartRead, TextureHandle frameStartWrite, bool doubleBuffered)
            {
                FrameStartRead = frameStartRead;
                FrameStartWrite = frameStartWrite;
                DoubleBuffered = doubleBuffered;
            }
        }
    }
}

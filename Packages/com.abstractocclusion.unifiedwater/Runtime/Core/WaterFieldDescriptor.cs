using System;
using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Immutable description of one water field: which layers exist, at what square
    /// resolution, and across how many cascade slices. Built from a quality tier and a
    /// domain, then handed to <see cref="WaterField"/> for allocation. Validated on
    /// construction so a malformed field fails immediately rather than rendering black.
    /// </summary>
    internal sealed class WaterFieldDescriptor
    {
        private readonly HashSet<WaterLayer> _layerSet;

        internal int Resolution { get; }
        internal int CascadeCount { get; }
        internal IReadOnlyList<WaterLayer> Layers { get; }

        internal WaterFieldDescriptor(int resolution, int cascadeCount, IReadOnlyList<WaterLayer> layers)
        {
            ValidateResolution(resolution);
            ValidateCascadeCount(cascadeCount);
            var uniqueLayers = ValidateLayers(layers);

            Resolution = resolution;
            CascadeCount = cascadeCount;
            Layers = uniqueLayers;
            _layerSet = new HashSet<WaterLayer>(uniqueLayers);
        }

        internal bool Contains(WaterLayer layer) => _layerSet.Contains(layer);

        private static void ValidateResolution(int resolution)
        {
            // Only generic bounds are enforced here. Layer-specific constraints (power-of-two
            // for FFT waves, a multiple of the sim thread-group for the ripple layer) belong to
            // the provider that owns the layer; the port's WaterQuality already rounds sim size.
            if (resolution < WaterFieldConstants.MinResolution || resolution > WaterFieldConstants.MaxResolution)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolution), resolution,
                    $"Resolution must be within [{WaterFieldConstants.MinResolution}, {WaterFieldConstants.MaxResolution}].");
            }
        }

        private static void ValidateCascadeCount(int cascadeCount)
        {
            if (cascadeCount < WaterFieldConstants.MinCascadeCount || cascadeCount > WaterFieldConstants.MaxCascadeCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cascadeCount), cascadeCount,
                    $"Cascade count must be within [{WaterFieldConstants.MinCascadeCount}, {WaterFieldConstants.MaxCascadeCount}].");
            }
        }

        private static IReadOnlyList<WaterLayer> ValidateLayers(IReadOnlyList<WaterLayer> layers)
        {
            if (layers == null)
            {
                throw new ArgumentNullException(nameof(layers));
            }

            if (layers.Count == 0)
            {
                throw new ArgumentException("A water field needs at least one layer.", nameof(layers));
            }

            // Defensive copy guarantees immutability and detects duplicate layer targets.
            var copy = new List<WaterLayer>(layers.Count);
            foreach (var layer in layers)
            {
                if (copy.Contains(layer))
                {
                    throw new ArgumentException($"Duplicate layer '{layer}' in field descriptor.", nameof(layers));
                }

                copy.Add(layer);
            }

            return copy;
        }
    }
}

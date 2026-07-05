using System.Collections.Generic;
using AbstractOcclusion.WebGpuWater;

namespace AbstractOcclusion.UnifiedWater.WebGpuBridge
{
    /// <summary>
    /// Adapts the port's proven <see cref="WaterQuality"/> tiers into a unified field
    /// descriptor, so the engine reuses the existing quality scaling instead of inventing a
    /// parallel one. Thin adapter only: it reads the resolved tier's sim resolution and hands
    /// the rest to <see cref="WaterFieldDescriptorFactory"/>.
    /// </summary>
    internal static class WaterQualityFieldPlanner
    {
        internal static WaterFieldDescriptor BuildBounded(WaterQuality quality, IReadOnlyList<WaterLayer> layers)
        {
            // A missing asset falls back to the port's Default tier, matching how the port
            // itself treats a body with no quality asset assigned.
            var tier = quality != null ? quality.Resolve() : WaterQuality.Default;
            return WaterFieldDescriptorFactory.CreateBounded(tier.SimResolution, layers);
        }
    }
}

using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Builds a <see cref="WaterFieldDescriptor"/> for a domain. It encodes the single rule
    /// that separates the two domain kinds: a bounded body (pond, pool, lake) is the depth-1
    /// case of the cascade system, so open-water support later means adding cascades rather
    /// than rewriting the pond path.
    /// </summary>
    internal static class WaterFieldDescriptorFactory
    {
        internal static WaterFieldDescriptor CreateBounded(int resolution, IReadOnlyList<WaterLayer> layers)
        {
            return new WaterFieldDescriptor(resolution, WaterFieldConstants.BoundedCascadeCount, layers);
        }
    }
}

using System;
using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// A bounded region of water. It owns one <see cref="WaterField"/> sized from a quality tier
    /// and the set of layers its providers need, and manages that field's lifetime. Thin
    /// coordinator: it builds the descriptor via <see cref="WaterFieldDescriptorFactory"/> and
    /// holds the field — no simulation or rendering logic. Layer validation is not repeated here;
    /// the descriptor is the single place that enforces it.
    /// </summary>
    internal sealed class WaterDomain : IDisposable
    {
        internal WaterField Field { get; }

        internal WaterDomain(WaterQualityTier tier, IReadOnlyList<WaterLayer> layers)
        {
            if (tier == null)
            {
                throw new ArgumentNullException(nameof(tier));
            }

            var descriptor = WaterFieldDescriptorFactory.CreateBounded(tier.FieldResolution, layers);
            Field = new WaterField(descriptor);
        }

        public void Dispose()
        {
            Field.Dispose();
        }
    }
}

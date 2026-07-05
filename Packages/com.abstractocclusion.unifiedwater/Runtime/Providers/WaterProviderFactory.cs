using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Builds the provider set a domain runs. This is the single place that knows the provider
    /// roster, so a domain never news up a provider directly and enabling a new effect is one edit
    /// here. Today it returns the ripple simulation; the obstacle and wave providers join here next,
    /// gated on tier and effect flags.
    /// </summary>
    internal static class WaterProviderFactory
    {
        internal static IReadOnlyList<IWaterFieldProvider> Create(WaterQualityTier tier)
        {
            return new IWaterFieldProvider[]
            {
                new RippleSimProvider(tier),
                new WaveComposeProvider()
            };
        }
    }
}

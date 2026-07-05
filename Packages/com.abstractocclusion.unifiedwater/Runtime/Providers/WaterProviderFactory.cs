using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Builds the provider set a domain runs. This is the single place that knows the provider
    /// roster, so a domain never news up a provider directly and enabling a new effect is one edit
    /// here. Today it returns only the diagnostic provider that proves the plumbing; the ripple and
    /// obstacle providers join here next, gated on tier and effect flags.
    /// </summary>
    internal static class WaterProviderFactory
    {
        internal static IReadOnlyList<IWaterFieldProvider> CreateDefault()
        {
            return new IWaterFieldProvider[] { new DiagnosticFieldProvider() };
        }
    }
}

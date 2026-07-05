using System;
using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// The set of water domains currently alive in the scene. Authoring components register their
    /// domain when enabled and remove it when disabled; the renderer feature reads this each frame
    /// instead of scanning the scene. Main-thread only — Unity component callbacks and the render
    /// loop are the only callers, so no synchronisation is needed.
    /// </summary>
    internal static class WaterDomainRegistry
    {
        private static readonly List<WaterDomain> RegisteredDomains = new List<WaterDomain>();

        internal static IReadOnlyList<WaterDomain> Domains => RegisteredDomains;

        internal static void Register(WaterDomain domain)
        {
            if (domain == null)
            {
                throw new ArgumentNullException(nameof(domain));
            }

            // Idempotent: a component re-enabled without a disable must not double-register.
            if (!RegisteredDomains.Contains(domain))
            {
                RegisteredDomains.Add(domain);
            }
        }

        internal static void Unregister(WaterDomain domain)
        {
            RegisteredDomains.Remove(domain);
        }
    }
}

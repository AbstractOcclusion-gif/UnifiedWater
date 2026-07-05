using System;
using System.Collections.Generic;
using System.Linq;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// A bounded region of water. It owns one <see cref="WaterField"/> whose layout is derived from
    /// the providers that write it, sized from a quality tier, and manages that field's and those
    /// providers' lifetime. Thin coordinator: it orders providers by their write position, unions
    /// their written layers into a field descriptor via <see cref="WaterFieldDescriptorFactory"/>,
    /// allocates the field, then lets each provider set up against it. No simulation or rendering
    /// logic lives here — providers write layers, this only wires them to a field.
    /// </summary>
    internal sealed class WaterDomain : IDisposable
    {
        internal WaterField Field { get; }

        /// <summary>Providers in canonical pipeline order (ascending <see cref="IWaterFieldProvider.WriteOrder"/>).</summary>
        internal IReadOnlyList<IWaterFieldProvider> Providers { get; }

        internal WaterDomain(WaterQualityTier tier, IReadOnlyList<IWaterFieldProvider> providers)
        {
            if (tier == null)
            {
                throw new ArgumentNullException(nameof(tier));
            }

            Providers = OrderProviders(providers);

            var layers = DeriveFieldLayers(Providers);
            var descriptor = WaterFieldDescriptorFactory.CreateBounded(tier.FieldResolution, layers);
            Field = new WaterField(descriptor);

            SetupProviders(Providers, descriptor);
        }

        public void Dispose()
        {
            foreach (var provider in Providers)
            {
                provider.Dispose();
            }

            Field.Dispose();
        }

        /// <summary>
        /// Validates the provider set and returns it stably ordered by write position. Kept static
        /// and pure so the ordering/validation rules are testable without allocating a GPU field.
        /// </summary>
        internal static IReadOnlyList<IWaterFieldProvider> OrderProviders(
            IReadOnlyList<IWaterFieldProvider> providers)
        {
            if (providers == null)
            {
                throw new ArgumentNullException(nameof(providers));
            }

            if (providers.Count == 0)
            {
                throw new ArgumentException("A water domain needs at least one provider.", nameof(providers));
            }

            var ordered = new List<IWaterFieldProvider>(providers.Count);
            foreach (var provider in providers)
            {
                if (provider == null)
                {
                    throw new ArgumentException("Provider set contains a null entry.", nameof(providers));
                }

                if (provider.WrittenLayers == null || provider.WrittenLayers.Count == 0)
                {
                    throw new ArgumentException(
                        $"Provider '{provider.GetType().Name}' declares no written layers.", nameof(providers));
                }

                ordered.Add(provider);
            }

            // OrderBy is a stable sort: providers sharing a WriteOrder keep registration order, so a
            // deliberate tie resolves predictably rather than by an unstable comparer.
            return ordered.OrderBy(provider => provider.WriteOrder).ToList();
        }

        /// <summary>
        /// Unions the ordered providers' written layers into the distinct layer set the field
        /// allocates. Several providers may share a layer (an ordered write-chain), so a repeat is
        /// de-duplicated here rather than rejected; first appearance wins the position. Static and
        /// pure for the same testability reason as <see cref="OrderProviders"/>.
        /// </summary>
        internal static IReadOnlyList<WaterLayer> DeriveFieldLayers(
            IReadOnlyList<IWaterFieldProvider> orderedProviders)
        {
            var seen = new HashSet<WaterLayer>();
            var layers = new List<WaterLayer>();
            foreach (var provider in orderedProviders)
            {
                foreach (var layer in provider.WrittenLayers)
                {
                    if (seen.Add(layer))
                    {
                        layers.Add(layer);
                    }
                }
            }

            return layers;
        }

        private static void SetupProviders(
            IReadOnlyList<IWaterFieldProvider> providers, WaterFieldDescriptor descriptor)
        {
            foreach (var provider in providers)
            {
                provider.Setup(descriptor);
            }
        }
    }
}

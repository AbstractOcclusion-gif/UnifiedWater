using System;
using System.Collections.Generic;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Writes one or more <see cref="WaterLayer"/>s of a domain's field. A provider declares the
    /// layers it targets and a deterministic position in the domain's write pipeline; the domain
    /// derives its field layout from the union of its providers' written layers. This seam carries
    /// only that declaration plus field-sized resource lifetime — render-graph pass wiring is
    /// layered on once the controller exists, so a provider stays a small, testable unit before any
    /// GPU code is written.
    /// </summary>
    internal interface IWaterFieldProvider : IDisposable
    {
        /// <summary>
        /// Layers this provider writes. Never empty. Several providers may target the same layer to
        /// form an ordered write-chain — for example ripple integration then obstacle injection both
        /// writing <see cref="WaterLayer.Dynamic"/>; <see cref="WriteOrder"/> fixes their sequence.
        /// </summary>
        IReadOnlyList<WaterLayer> WrittenLayers { get; }

        /// <summary>
        /// Position in the domain's write pipeline. Lower runs first. Deterministic so a shared-layer
        /// chain has one unambiguous order instead of depending on registration accident.
        /// </summary>
        int WriteOrder { get; }

        /// <summary>
        /// Allocates provider-local resources sized to the field. Called once, after the field is
        /// allocated and before any frame runs. Throws on a field it cannot service, so a
        /// misconfigured provider fails fast at this boundary rather than producing black water.
        /// </summary>
        void Setup(WaterFieldDescriptor descriptor);

        /// <summary>
        /// Records this provider's render-graph pass(es) for one frame, reading and writing the
        /// imported layer handles the frame exposes. The provider never imports or swaps the field
        /// itself — the controller owns that — so a provider stays a pure pass contributor.
        /// </summary>
        void RecordPasses(in WaterFieldFrame frame);
    }
}

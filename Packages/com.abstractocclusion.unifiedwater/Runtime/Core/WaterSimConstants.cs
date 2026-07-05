namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Bounds and defaults for the ripple simulation's tunable values. The concrete per-quality
    /// numbers live on the tier asset; this fixes the ranges those fields clamp to and the starting
    /// values, so no sim literal is inlined in the tier or the kernels.
    /// </summary>
    internal static class WaterSimConstants
    {
        internal const float MinDamping = 0.9f;
        internal const float MaxDamping = 0.9999f;
        internal const float DefaultDamping = 0.99f;

        // Per-step coupling coefficient of the 4-neighbour scheme. Above ~0.5 the explicit
        // integrator is unstable (CFL), so that is the hard upper bound.
        internal const float MinPropagationSpeed = 0.05f;
        internal const float MaxPropagationSpeed = 0.5f;
        internal const float DefaultPropagationSpeed = 0.4f;

        /// <summary>Upper bound on impulses consumed in one frame; sizes the GPU impulse buffer.</summary>
        internal const int MaxImpulsesPerFrame = 64;

        // Fixed simulation timestep (seconds) and the per-frame substep cap. The sim advances in
        // whole steps of this size so behaviour is identical regardless of render framerate; the cap
        // stops a slow frame from triggering an unrecoverable catch-up spiral.
        internal const float MinFixedDeltaTime = 0.001f;
        internal const float MaxFixedDeltaTime = 0.1f;
        internal const float DefaultFixedDeltaTime = 1f / 60f;
        internal const int MaxSubstepsPerFrame = 8;

        // Authoring defaults and clamps for a water volume's footprint and its demo drops.
        internal const float MinDomainSizeMeters = 0.1f;
        internal const float DefaultDomainSizeMeters = 10f;
        internal const float MinImpulseRadiusMeters = 0.01f;
        internal const float DefaultImpulseRadiusMeters = 0.5f;
        internal const float DefaultImpulseStrength = 1f;
    }
}

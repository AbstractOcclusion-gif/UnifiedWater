namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Allocation limits, fixed texture settings, and naming suffixes for the Water Field.
    /// Tier assets choose concrete resolutions and cascade counts within these bounds;
    /// nothing here is a per-quality tuning value (those belong to the tier asset).
    /// </summary>
    internal static class WaterFieldConstants
    {
        internal const int MinResolution = 16;
        internal const int MaxResolution = 2048;

        /// <summary>Balanced default field resolution used when a tier does not specify one.</summary>
        internal const int DefaultResolution = 256;

        internal const int MinCascadeCount = 1;
        internal const int MaxCascadeCount = 8;

        /// <summary>A bounded domain (pond/pool/lake) is the depth-1 case of the cascade system.</summary>
        internal const int BoundedCascadeCount = 1;

        // Fixed properties of every field texture: single-sampled, no mipmaps, linear data.
        internal const int NoMultiSampling = 1;
        internal const int NoDepthBuffer = 0;

        // Ping-pong buffer name suffixes, kept out of code as strings are banned inline.
        internal const string PrimaryBufferSuffix = "_A";
        internal const string SecondaryBufferSuffix = "_B";
    }
}

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Tuning constants for the on-screen field debug view. Named here so the feature's inspector
    /// defaults and clamps carry no inline literals.
    /// </summary>
    internal static class WaterDebugConstants
    {
        // The diagnostic provider adds 1 per frame to the field, so a small scale maps a few dozen
        // frames onto the black→white range — enough to see the field advancing.
        internal const float DefaultValueScale = 0.02f;
        internal const float MinValueScale = 0.0001f;
    }
}

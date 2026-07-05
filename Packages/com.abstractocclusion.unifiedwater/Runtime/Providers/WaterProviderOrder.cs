namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Fixed write-pipeline positions for field providers. Lower runs first. Values are spaced so a
    /// new provider can slot between two existing ones without renumbering. This is the single home
    /// for the order of the Dynamic write-chain (diagnostic today; ripple then obstacle next).
    /// </summary>
    internal static class WaterProviderOrder
    {
        internal const int Diagnostic = 0;
        internal const int RippleSim = 100;
        internal const int ObstacleInject = 200;
        internal const int WaveCompose = 300;
    }
}

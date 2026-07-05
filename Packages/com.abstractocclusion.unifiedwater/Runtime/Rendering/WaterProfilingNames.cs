namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// GPU profiling / render-graph pass names. Kept as named constants so no pass-name string
    /// literal is scattered through the rendering code and the profiler labels stay consistent.
    /// </summary>
    internal static class WaterProfilingNames
    {
        internal const string DiagnosticAdvance = "Water Diagnostic Advance";
        internal const string FieldUpdate = "Water Field Update";
        internal const string FieldDebugView = "Water Field Debug View";
    }
}

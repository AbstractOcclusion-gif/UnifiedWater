using System.Runtime.CompilerServices;

// The editor and test assemblies operate on internal types by design, so the
// runtime keeps its public surface minimal (see RULES.md: minimize public API).
[assembly: InternalsVisibleTo("AbstractOcclusion.UnifiedWater.Editor")]
[assembly: InternalsVisibleTo("AbstractOcclusion.UnifiedWater.Tests")]

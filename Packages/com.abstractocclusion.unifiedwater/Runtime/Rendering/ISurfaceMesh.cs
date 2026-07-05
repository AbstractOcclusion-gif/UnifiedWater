using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Builds the water surface geometry for a bounded domain. This is a seam, not a detail: the
    /// bounded pond is a single-density subdivided plane, while a Phase-2 camera-centred clipmap/LOD
    /// grid for large or infinite water is a different implementation behind the same contract, so the
    /// surface shader never has to know which one produced the mesh. Callers own the returned mesh.
    /// </summary>
    internal interface ISurfaceMesh
    {
        /// <summary>
        /// Builds a surface mesh spanning a square footprint of <paramref name="sizeMeters"/> centred
        /// on local origin, subdivided into <paramref name="quadsPerSide"/> quads along each axis.
        /// </summary>
        Mesh Build(float sizeMeters, int quadsPerSide);
    }
}

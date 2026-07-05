namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Geometry budget and fixed dimensions for the procedural water surface mesh. Sole home for the
    /// surface mesh's literal values, so neither the quality tier nor the mesh builder hardcodes them.
    /// </summary>
    internal static class WaterSurfaceConstants
    {
        // Quads per side of the subdivided plane. Deliberately independent of field texture resolution:
        // geometry density trades vertex cost, texel density trades sim cost, and they tune separately.
        internal const int MinSurfaceMeshResolution = 2;
        internal const int MaxSurfaceMeshResolution = 512;
        internal const int DefaultSurfaceMeshResolution = 128;

        // The plane is built in local space centred on the object's origin at this height; the volume
        // transform's Y then places the surface (locked decision: surface Y = the transform's Y).
        internal const float LocalSurfaceHeight = 0f;

        // Half the footprint, so a side spans [-Half, +Half] * sizeMeters around the centre.
        internal const float HalfExtentFactor = 0.5f;

        // uv spans the whole footprint so the surface samples the field across its full extent.
        internal const float UvMin = 0f;
        internal const float UvMax = 1f;

        // Above this vertex count a 16-bit index buffer overflows and the mesh needs 32-bit indices.
        internal const int MaxSixteenBitVertexCount = 65535;

        // The plane's flat bounds would let the GPU frustum-cull it the instant a wave lifts a vertex
        // above or below zero, since the shader displaces height only on the GPU. Padding the vertical
        // extent by this margin (metres, each side) keeps it drawn. Sized for a pond; a look driving a
        // large _HeightScale that visibly clips at the frustum edge would raise it.
        internal const float SurfaceBoundsHeightMargin = 2f;
    }
}

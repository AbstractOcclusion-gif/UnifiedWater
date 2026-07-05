namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Identity of a single data layer in the Water Field. Every provider writes,
    /// and every consumer reads, the field through these layers. Formats and
    /// buffering strategy per layer live in <see cref="WaterLayerCatalog"/>.
    /// </summary>
    internal enum WaterLayer
    {
        Displacement,
        Dynamic,
        SurfaceNormalFoam,
        Depth,
        Flow,
        Absorption
    }
}

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Fixed budgets and defaults for planar water reflection. Sole home for its literal values so the
    /// manager hardcodes none of them.
    /// </summary>
    internal static class WaterReflectionConstants
    {
        // Reflection render-target size as a fraction of the source camera's pixel size.
        internal const float DefaultResolutionScale = 0.5f;
        internal const float MinResolutionScale = 0.1f;
        internal const float MaxResolutionScale = 1f;

        // Small push of the clip plane along the surface normal so ripples that dip just below the
        // mathematical water plane don't reveal a seam of clipped geometry at the shoreline.
        internal const float ClipPlaneOffset = 0.02f;

        // The reflection camera renders opaque geometry, so its target needs a depth buffer.
        internal const int CameraTargetDepthBits = 24;

        // RenderGraph refuses to import a texture that carries both colour and depth, so the camera
        // target's colour is copied into this colour-only texture before it becomes the surface global.
        internal const int PublishedColorDepthBits = 0;

        // Floor for the reflection target dimensions so a tiny viewport never allocates a zero-size texture.
        internal const int MinReflectionSize = 16;

        internal const string ReflectionCameraName = "UnifiedWater Reflection Camera (hidden)";
        internal const string ReflectionTextureName = "UnifiedWater Planar Reflection";
    }
}

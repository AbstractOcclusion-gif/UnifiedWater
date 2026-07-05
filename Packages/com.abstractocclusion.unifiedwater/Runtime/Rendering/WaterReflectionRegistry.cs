using UnityEngine.Rendering;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Holds the current frame's planar reflection target so the field publisher can bind it as a
    /// global for the surface shader, without the publisher needing to know about the reflection
    /// camera. The manager sets this each frame it renders and clears it when disabled. Main-thread
    /// only — camera-render callbacks and the render loop are the only callers.
    /// </summary>
    internal static class WaterReflectionRegistry
    {
        internal static RTHandle Current { get; private set; }

        internal static void Set(RTHandle reflection)
        {
            Current = reflection;
        }

        // Clear only if the caller still owns the slot, so a disabled manager can't wipe a handle a
        // different manager has since published.
        internal static void Clear(RTHandle reflection)
        {
            if (Current == reflection)
            {
                Current = null;
            }
        }
    }
}

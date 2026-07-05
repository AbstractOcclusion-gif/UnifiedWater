using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Authoring asset holding the GPU-cost budget for a water domain. It currently carries the
    /// field resolution; further per-tier budgets (cascade count, god-ray steps, foam caps) are
    /// added here as the passes that consume them are built. One asset can be shared by many
    /// domains. This is the single home for tier budget numbers — nothing else hardcodes them.
    /// </summary>
    [CreateAssetMenu(
        fileName = "WaterQualityTier",
        menuName = "AbstractOcclusion/Unified Water/Quality Tier")]
    public sealed class WaterQualityTier : ScriptableObject
    {
        [Tooltip("Square resolution, in texels, of every field-layer texture this tier allocates.")]
        [SerializeField]
        [Range(WaterFieldConstants.MinResolution, WaterFieldConstants.MaxResolution)]
        private int fieldResolution = WaterFieldConstants.DefaultResolution;

        [Tooltip("Per-step energy retention of the ripple sim. Lower settles faster.")]
        [SerializeField]
        [Range(WaterSimConstants.MinDamping, WaterSimConstants.MaxDamping)]
        private float rippleDamping = WaterSimConstants.DefaultDamping;

        [Tooltip("Per-step wave coupling of the ripple sim. Above ~0.5 the sim becomes unstable.")]
        [SerializeField]
        [Range(WaterSimConstants.MinPropagationSpeed, WaterSimConstants.MaxPropagationSpeed)]
        private float propagationSpeed = WaterSimConstants.DefaultPropagationSpeed;

        [Tooltip("Fixed timestep of the ripple sim, in seconds. Smaller is more accurate but costlier.")]
        [SerializeField]
        [Range(WaterSimConstants.MinFixedDeltaTime, WaterSimConstants.MaxFixedDeltaTime)]
        private float simFixedDeltaTime = WaterSimConstants.DefaultFixedDeltaTime;

        [Tooltip("Quads per side of the procedural surface mesh. Independent of field resolution.")]
        [SerializeField]
        [Range(WaterSurfaceConstants.MinSurfaceMeshResolution, WaterSurfaceConstants.MaxSurfaceMeshResolution)]
        private int surfaceMeshResolution = WaterSurfaceConstants.DefaultSurfaceMeshResolution;

        public int FieldResolution => fieldResolution;

        internal float RippleDamping => rippleDamping;

        internal float PropagationSpeed => propagationSpeed;

        internal float SimFixedDeltaTime => simFixedDeltaTime;

        internal int SurfaceMeshResolution => surfaceMeshResolution;
    }
}

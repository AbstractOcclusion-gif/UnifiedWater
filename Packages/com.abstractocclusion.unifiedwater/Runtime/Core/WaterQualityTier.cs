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

        public int FieldResolution => fieldResolution;
    }
}

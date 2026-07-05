using System;
using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Authoring entry point: add one to a GameObject to create a bounded water domain in the scene.
    /// Deliberately thin — it holds the quality tier, owns the domain's lifetime, and registers it
    /// for rendering. No simulation, rendering, or resource logic lives here; the domain and its
    /// providers do that. The domain is built on enable and disposed on disable so play/edit toggles
    /// never leak GPU textures.
    /// </summary>
    [AddComponentMenu("AbstractOcclusion/Unified Water/Water Volume")]
    public sealed class WaterVolume : MonoBehaviour
    {
        [Tooltip("Quality tier that sizes this domain's field. Required.")]
        [SerializeField]
        private WaterQualityTier tier;

        private WaterDomain _domain;

        internal WaterDomain Domain => _domain;

        private void OnEnable()
        {
            if (tier == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WaterVolume)} on '{name}' has no {nameof(WaterQualityTier)} assigned.");
            }

            _domain = new WaterDomain(tier, WaterProviderFactory.CreateDefault());
            WaterDomainRegistry.Register(_domain);
        }

        private void OnDisable()
        {
            if (_domain == null)
            {
                return;
            }

            WaterDomainRegistry.Unregister(_domain);
            _domain.Dispose();
            _domain = null;
        }
    }
}

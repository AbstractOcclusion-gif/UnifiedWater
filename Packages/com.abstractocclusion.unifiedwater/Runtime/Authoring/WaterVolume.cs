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

        [Tooltip("Square world size, in metres, of the water footprint centred on this transform.")]
        [SerializeField]
        [Min(WaterSimConstants.MinDomainSizeMeters)]
        private float sizeMeters = WaterSimConstants.DefaultDomainSizeMeters;

        private WaterDomain _domain;

        internal WaterDomain Domain => _domain;

        /// <summary>Square world size, in metres, of this domain's footprint. The surface mesh spans it.</summary>
        internal float SizeMeters => sizeMeters;

        /// <summary>
        /// Surface mesh density (quads per side) from the assigned tier, falling back to the default
        /// when no tier is set yet so an in-editor surface can build before the tier is wired.
        /// </summary>
        internal int SurfaceMeshResolution =>
            tier != null ? tier.SurfaceMeshResolution : WaterSurfaceConstants.DefaultSurfaceMeshResolution;

        /// <summary>
        /// Drops a ripple at a world position. Gameplay and the demo interactor call this; it converts
        /// to the field's uv space via the domain extent, so callers never touch texels. No-op until
        /// the volume is enabled.
        /// </summary>
        public void InjectImpulse(Vector3 worldPosition, float radiusMeters, float strength)
        {
            if (_domain == null)
            {
                return;
            }

            var centerUv = _domain.Extent.WorldToUv(worldPosition);
            float radiusUv = radiusMeters / sizeMeters;
            _domain.InjectImpulse(new WaterImpulse(centerUv, radiusUv, strength));
        }

        private void Update()
        {
            _domain?.Advance(Time.deltaTime);
        }

        private void OnEnable()
        {
            if (tier == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WaterVolume)} on '{name}' has no {nameof(WaterQualityTier)} assigned.");
            }

            var position = transform.position;
            var extent = new BoundedDomainExtent(
                new Vector2(position.x, position.z), sizeMeters, tier.FieldResolution);

            _domain = new WaterDomain(tier, WaterProviderFactory.Create(tier), extent);
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

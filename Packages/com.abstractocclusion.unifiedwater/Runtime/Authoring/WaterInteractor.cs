using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Demo input: on a left click, projects the cursor onto the water plane and drops a ripple.
    /// A convenience for trying a <see cref="WaterVolume"/> in a scene — gameplay would call
    /// <see cref="WaterVolume.InjectImpulse"/> directly. Requires a volume on the same GameObject and
    /// the legacy Input Manager to be enabled.
    /// </summary>
    [RequireComponent(typeof(WaterVolume))]
    [AddComponentMenu("AbstractOcclusion/Unified Water/Water Interactor (Demo)")]
    public sealed class WaterInteractor : MonoBehaviour
    {
        [Tooltip("World radius of each dropped ripple.")]
        [SerializeField]
        [Min(WaterSimConstants.MinImpulseRadiusMeters)]
        private float dropRadiusMeters = WaterSimConstants.DefaultImpulseRadiusMeters;

        [Tooltip("Height added at the centre of each drop.")]
        [SerializeField]
        private float dropStrength = WaterSimConstants.DefaultImpulseStrength;

        [Tooltip("Camera used to project the cursor. Falls back to Camera.main when unset.")]
        [SerializeField]
        private Camera sourceCamera;

        private WaterVolume _volume;

        private void Awake()
        {
            _volume = GetComponent<WaterVolume>();
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
            if (activeCamera == null)
            {
                return;
            }

            if (TryProjectToWaterPlane(activeCamera, out var worldPoint))
            {
                _volume.InjectImpulse(worldPoint, dropRadiusMeters, dropStrength);
            }
        }

        // The water surface is the horizontal plane through this transform's height.
        private bool TryProjectToWaterPlane(Camera activeCamera, out Vector3 worldPoint)
        {
            var surface = new Plane(Vector3.up, transform.position);
            var ray = activeCamera.ScreenPointToRay(Input.mousePosition);
            if (surface.Raycast(ray, out float distance))
            {
                worldPoint = ray.GetPoint(distance);
                return true;
            }

            worldPoint = default;
            return false;
        }
    }
}

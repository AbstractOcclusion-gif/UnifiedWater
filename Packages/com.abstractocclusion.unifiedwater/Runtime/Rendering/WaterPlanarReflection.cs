using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Renders a planar reflection of the scene for the water surface. For each real camera it mirrors
    /// that camera across the water plane (the volume's Y, up normal), clips everything below the
    /// surface with an oblique near plane, and renders the mirrored view into a texture that the field
    /// publisher exposes to the surface shader as the <c>_UnifiedWater_PlanarReflection</c> global.
    /// Composition, not inheritance: a separate component so the volume stays a pure simulation entry
    /// point, and the surface picks planar over sky purely by a material keyword.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(WaterVolume))]
    [AddComponentMenu("AbstractOcclusion/Unified Water/Water Planar Reflection")]
    public sealed class WaterPlanarReflection : MonoBehaviour
    {
        [Tooltip("Layers the reflection renders. Exclude the water's own layer to avoid self-recursion.")]
        [SerializeField]
        private LayerMask reflectedLayers = ~0;

        [Tooltip("Reflection resolution as a fraction of the source camera's pixel size.")]
        [SerializeField]
        [Range(WaterReflectionConstants.MinResolutionScale, WaterReflectionConstants.MaxResolutionScale)]
        private float resolutionScale = WaterReflectionConstants.DefaultResolutionScale;

        private Camera _reflectionCamera;
        private RenderTexture _reflectionTexture;      // camera target: colour + depth
        private RenderTexture _reflectionColorTexture; // colour-only copy the render graph imports
        private RTHandle _reflectionColorHandle;
        private bool _isRendering;

        private void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ReleaseTexture();
            ReleaseCamera();
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera sourceCamera)
        {
            if (!ShouldReflectFor(sourceCamera))
            {
                return;
            }

            _isRendering = true;
            try
            {
                RenderReflection(sourceCamera);
            }
            finally
            {
                _isRendering = false;
            }
        }

        // Reflect only for cameras actually viewing the scene, and never for our own reflection camera
        // or re-entrantly — that is the recursion guard.
        private bool ShouldReflectFor(Camera sourceCamera)
        {
            if (_isRendering || sourceCamera == _reflectionCamera)
            {
                return false;
            }

            return sourceCamera.cameraType == CameraType.Game
                || sourceCamera.cameraType == CameraType.SceneView;
        }

        private void RenderReflection(Camera sourceCamera)
        {
            EnsureCamera();
            EnsureTexture(sourceCamera);

            var planeNormal = Vector3.up;
            var planePoint = new Vector3(
                0f, transform.position.y + WaterReflectionConstants.ClipPlaneOffset, 0f);

            MirrorCameraAcrossPlane(sourceCamera, planePoint, planeNormal);

            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _reflectionTexture };

            // Mirroring flips triangle winding; invert culling for the duration of the synchronous render.
            bool previousInvertCulling = GL.invertCulling;
            GL.invertCulling = true;
            RenderPipeline.SubmitRenderRequest(_reflectionCamera, request);
            GL.invertCulling = previousInvertCulling;

            // Copy the colour out of the camera target so the published texture carries no depth format.
            Graphics.Blit(_reflectionTexture, _reflectionColorTexture);
            WaterReflectionRegistry.Set(_reflectionColorHandle);
        }

        private void MirrorCameraAcrossPlane(Camera sourceCamera, Vector3 planePoint, Vector3 planeNormal)
        {
            _reflectionCamera.CopyFrom(sourceCamera);
            _reflectionCamera.enabled = false;
            _reflectionCamera.cullingMask = reflectedLayers;

            float planeDistance = -Vector3.Dot(planeNormal, planePoint);
            var plane = new Vector4(planeNormal.x, planeNormal.y, planeNormal.z, planeDistance);

            _reflectionCamera.worldToCameraMatrix = sourceCamera.worldToCameraMatrix * ReflectionMatrix(plane);

            // Oblique near plane at the water surface so nothing below it leaks into the reflection.
            Vector4 clipPlane = CameraSpacePlane(_reflectionCamera, planePoint, planeNormal);
            _reflectionCamera.projectionMatrix = sourceCamera.CalculateObliqueMatrix(clipPlane);
        }

        private void EnsureCamera()
        {
            if (_reflectionCamera != null)
            {
                return;
            }

            var host = new GameObject(WaterReflectionConstants.ReflectionCameraName)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _reflectionCamera = host.AddComponent<Camera>();
            _reflectionCamera.enabled = false;
        }

        private void EnsureTexture(Camera sourceCamera)
        {
            int width = ReflectionExtent(sourceCamera.pixelWidth);
            int height = ReflectionExtent(sourceCamera.pixelHeight);
            if (_reflectionTexture != null
                && _reflectionTexture.width == width
                && _reflectionTexture.height == height)
            {
                return;
            }

            ReleaseTexture();
            _reflectionTexture = CreateTarget(
                width, height, WaterReflectionConstants.CameraTargetDepthBits);
            _reflectionColorTexture = CreateTarget(
                width, height, WaterReflectionConstants.PublishedColorDepthBits);
            _reflectionColorHandle = RTHandles.Alloc(_reflectionColorTexture);
        }

        private static RenderTexture CreateTarget(int width, int height, int depthBits)
        {
            var target = new RenderTexture(width, height, depthBits, RenderTextureFormat.DefaultHDR)
            {
                name = WaterReflectionConstants.ReflectionTextureName
            };
            target.Create();
            return target;
        }

        private int ReflectionExtent(int sourcePixels)
        {
            return Mathf.Max(
                WaterReflectionConstants.MinReflectionSize, Mathf.RoundToInt(sourcePixels * resolutionScale));
        }

        private void ReleaseTexture()
        {
            WaterReflectionRegistry.Clear(_reflectionColorHandle);

            if (_reflectionColorHandle != null)
            {
                _reflectionColorHandle.Release();
                _reflectionColorHandle = null;
            }

            if (_reflectionColorTexture != null)
            {
                DestroyObject(_reflectionColorTexture);
                _reflectionColorTexture = null;
            }

            if (_reflectionTexture != null)
            {
                DestroyObject(_reflectionTexture);
                _reflectionTexture = null;
            }
        }

        private void ReleaseCamera()
        {
            if (_reflectionCamera == null)
            {
                return;
            }

            DestroyObject(_reflectionCamera.gameObject);
            _reflectionCamera = null;
        }

        private static void DestroyObject(Object target)
        {
            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }

        // Reflects world points across the plane (nx, ny, nz, d), where n·p + d = 0.
        private static Matrix4x4 ReflectionMatrix(Vector4 plane)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = 1f - 2f * plane.x * plane.x;
            matrix.m01 = -2f * plane.x * plane.y;
            matrix.m02 = -2f * plane.x * plane.z;
            matrix.m03 = -2f * plane.x * plane.w;
            matrix.m10 = -2f * plane.y * plane.x;
            matrix.m11 = 1f - 2f * plane.y * plane.y;
            matrix.m12 = -2f * plane.y * plane.z;
            matrix.m13 = -2f * plane.y * plane.w;
            matrix.m20 = -2f * plane.z * plane.x;
            matrix.m21 = -2f * plane.z * plane.y;
            matrix.m22 = 1f - 2f * plane.z * plane.z;
            matrix.m23 = -2f * plane.z * plane.w;
            return matrix;
        }

        // The water plane expressed in the reflection camera's space, for the oblique projection.
        private static Vector4 CameraSpacePlane(Camera camera, Vector3 pointWS, Vector3 normalWS)
        {
            Matrix4x4 worldToCamera = camera.worldToCameraMatrix;
            Vector3 pointCS = worldToCamera.MultiplyPoint(pointWS);
            Vector3 normalCS = worldToCamera.MultiplyVector(normalWS).normalized;
            return new Vector4(normalCS.x, normalCS.y, normalCS.z, -Vector3.Dot(pointCS, normalCS));
        }
    }
}

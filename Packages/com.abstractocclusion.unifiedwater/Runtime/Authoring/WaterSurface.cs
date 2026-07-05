using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Authoring: gives a <see cref="WaterVolume"/> a visible surface. Builds a procedural plane sized
    /// to the volume's footprint and hands it to the MeshFilter; the MeshRenderer's own material (the
    /// water shader, assigned there) samples the published field globals to displace and shade it.
    /// Kept out of WaterVolume so the domain stays a pure simulation authoring point and the surface is
    /// a composable concern. Owns the generated mesh's lifetime, so edit/play toggles never leak it.
    ///
    /// The size and density that shape the mesh live on the volume and its quality tier, not on this
    /// component, so editing them never fires this component's OnValidate. It therefore runs in the
    /// editor (ExecuteAlways) and polls those values each frame, rebuilding only when they change.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(WaterVolume), typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("AbstractOcclusion/Unified Water/Water Surface")]
    public sealed class WaterSurface : MonoBehaviour
    {
        private readonly ISurfaceMesh _surfaceMesh = new PlaneSurfaceMesh();

        private MeshFilter _meshFilter;
        private Mesh _generatedMesh;
        private float _builtSizeMeters;
        private int _builtQuadsPerSide;

        private void OnEnable()
        {
            RebuildIfNeeded();
        }

        private void OnDisable()
        {
            ReleaseMesh();
        }

        private void Update()
        {
            RebuildIfNeeded();
        }

        private void RebuildIfNeeded()
        {
            var volume = GetComponent<WaterVolume>();
            if (volume == null)
            {
                return;
            }

            float sizeMeters = volume.SizeMeters;
            int quadsPerSide = volume.SurfaceMeshResolution;
            if (sizeMeters <= 0f || quadsPerSide < WaterSurfaceConstants.MinSurfaceMeshResolution)
            {
                return;
            }

            if (!RebuildRequired(sizeMeters, quadsPerSide))
            {
                return;
            }

            Rebuild(sizeMeters, quadsPerSide);
        }

        private bool RebuildRequired(float sizeMeters, int quadsPerSide)
        {
            return _generatedMesh == null
                || sizeMeters != _builtSizeMeters
                || quadsPerSide != _builtQuadsPerSide;
        }

        private void Rebuild(float sizeMeters, int quadsPerSide)
        {
            ReleaseMesh();
            _generatedMesh = _surfaceMesh.Build(sizeMeters, quadsPerSide);
            _builtSizeMeters = sizeMeters;
            _builtQuadsPerSide = quadsPerSide;
            EnsureMeshFilter().sharedMesh = _generatedMesh;
        }

        private MeshFilter EnsureMeshFilter()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            return _meshFilter;
        }

        private void ReleaseMesh()
        {
            if (_generatedMesh == null)
            {
                return;
            }

            var meshFilter = EnsureMeshFilter();
            if (meshFilter.sharedMesh == _generatedMesh)
            {
                meshFilter.sharedMesh = null;
            }

            DestroyGeneratedMesh();
            _generatedMesh = null;
        }

        private void DestroyGeneratedMesh()
        {
            if (Application.isPlaying)
            {
                Destroy(_generatedMesh);
                return;
            }

            DestroyImmediate(_generatedMesh);
        }
    }
}

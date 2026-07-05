using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// The bounded-pond surface: a flat, uniformly subdivided plane sized to the domain footprint.
    /// Vertices sit in local space centred on origin at a fixed height so the volume transform places
    /// the surface; uv spans 0..1 across the footprint so the surface shader samples the field with
    /// the same mapping BoundedDomainExtent publishes. Stateless — one fresh plane per call.
    /// </summary>
    internal sealed class PlaneSurfaceMesh : ISurfaceMesh
    {
        private const string MeshName = "UnifiedWater Surface (Plane)";
        private const int IndicesPerQuad = 6;

        public Mesh Build(float sizeMeters, int quadsPerSide)
        {
            if (sizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeMeters), sizeMeters, "Surface size must be positive.");
            }

            if (quadsPerSide < WaterSurfaceConstants.MinSurfaceMeshResolution)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(quadsPerSide), quadsPerSide,
                    $"Surface needs at least {WaterSurfaceConstants.MinSurfaceMeshResolution} quads per side.");
            }

            int verticesPerSide = quadsPerSide + 1;
            var vertices = BuildVertices(sizeMeters, verticesPerSide, out var uvs, out var normals);
            var indices = BuildIndices(quadsPerSide, verticesPerSide);

            return Assemble(vertices, uvs, normals, indices);
        }

        private static Vector3[] BuildVertices(
            float sizeMeters, int verticesPerSide, out Vector2[] uvs, out Vector3[] normals)
        {
            int count = verticesPerSide * verticesPerSide;
            var vertices = new Vector3[count];
            uvs = new Vector2[count];
            normals = new Vector3[count];

            float half = sizeMeters * WaterSurfaceConstants.HalfExtentFactor;
            int lastIndex = verticesPerSide - 1;

            for (int row = 0; row < verticesPerSide; row++)
            {
                float rowFraction = (float)row / lastIndex;
                for (int column = 0; column < verticesPerSide; column++)
                {
                    float columnFraction = (float)column / lastIndex;
                    int vertex = row * verticesPerSide + column;

                    vertices[vertex] = new Vector3(
                        Mathf.Lerp(-half, half, columnFraction),
                        WaterSurfaceConstants.LocalSurfaceHeight,
                        Mathf.Lerp(-half, half, rowFraction));
                    uvs[vertex] = new Vector2(
                        Mathf.Lerp(WaterSurfaceConstants.UvMin, WaterSurfaceConstants.UvMax, columnFraction),
                        Mathf.Lerp(WaterSurfaceConstants.UvMin, WaterSurfaceConstants.UvMax, rowFraction));
                    normals[vertex] = Vector3.up;
                }
            }

            return vertices;
        }

        // Two triangles per quad, wound so the surface faces up (+Y): columns run +X, rows run +Z.
        private static int[] BuildIndices(int quadsPerSide, int verticesPerSide)
        {
            var indices = new int[quadsPerSide * quadsPerSide * IndicesPerQuad];
            int cursor = 0;

            for (int row = 0; row < quadsPerSide; row++)
            {
                for (int column = 0; column < quadsPerSide; column++)
                {
                    int bottomLeft = row * verticesPerSide + column;
                    int bottomRight = bottomLeft + 1;
                    int topLeft = bottomLeft + verticesPerSide;
                    int topRight = topLeft + 1;

                    indices[cursor++] = bottomLeft;
                    indices[cursor++] = topLeft;
                    indices[cursor++] = topRight;

                    indices[cursor++] = bottomLeft;
                    indices[cursor++] = topRight;
                    indices[cursor++] = bottomRight;
                }
            }

            return indices;
        }

        private static Mesh Assemble(Vector3[] vertices, Vector2[] uvs, Vector3[] normals, int[] indices)
        {
            var mesh = new Mesh { name = MeshName };
            if (vertices.Length > WaterSurfaceConstants.MaxSixteenBitVertexCount)
            {
                mesh.indexFormat = IndexFormat.UInt32;
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

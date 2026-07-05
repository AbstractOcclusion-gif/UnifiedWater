using System;
using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// The horizontal world footprint of a bounded water domain and its mapping to field texels: a
    /// square region of <see cref="SizeMeters"/> metres centred on <see cref="Center"/> (world X/Z),
    /// backed by a <see cref="Resolution"/>-texel field. Sole owner of the affine world↔uv mapping —
    /// the identical formula is mirrored in HLSL through published uniforms when a shader needs it,
    /// kept trivial so the two cannot drift. Immutable value.
    /// </summary>
    internal readonly struct BoundedDomainExtent
    {
        private const float UvCentre = 0.5f;

        internal Vector2 Center { get; }
        internal float SizeMeters { get; }
        internal int Resolution { get; }

        internal BoundedDomainExtent(Vector2 center, float sizeMeters, int resolution)
        {
            if (sizeMeters <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeMeters), sizeMeters, "Domain size must be positive.");
            }

            if (resolution <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(resolution), resolution, "Resolution must be positive.");
            }

            Center = center;
            SizeMeters = sizeMeters;
            Resolution = resolution;
        }

        /// <summary>World metres spanned by a single field texel.</summary>
        internal float TexelSizeMeters => SizeMeters / Resolution;

        /// <summary>Maps a world position (its X/Z) to field uv; within [0,1] inside the footprint.</summary>
        internal Vector2 WorldToUv(Vector3 worldPosition)
        {
            float u = (worldPosition.x - Center.x) / SizeMeters + UvCentre;
            float v = (worldPosition.z - Center.y) / SizeMeters + UvCentre;
            return new Vector2(u, v);
        }

        /// <summary>Maps field uv back to a world X/Z position.</summary>
        internal Vector2 UvToWorld(Vector2 uv)
        {
            float x = Center.x + (uv.x - UvCentre) * SizeMeters;
            float z = Center.y + (uv.y - UvCentre) * SizeMeters;
            return new Vector2(x, z);
        }
    }
}

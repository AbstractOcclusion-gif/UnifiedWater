using UnityEngine;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// A ripple disturbance in field uv space: a smooth cosine bump of <see cref="RadiusUv"/> centred
    /// at <see cref="CenterUv"/> that adds <see cref="Strength"/> to the water height. Producers work
    /// in world space and convert through <see cref="BoundedDomainExtent"/> before queueing, so the
    /// sim only ever sees uv.
    /// </summary>
    internal readonly struct WaterImpulse
    {
        internal Vector2 CenterUv { get; }
        internal float RadiusUv { get; }
        internal float Strength { get; }

        internal WaterImpulse(Vector2 centerUv, float radiusUv, float strength)
        {
            CenterUv = centerUv;
            RadiusUv = radiusUv;
            Strength = strength;
        }
    }
}

using System;
using UnityEngine.Experimental.Rendering;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Single source of truth for the static facts about each water layer: its GPU
    /// format and whether it needs double buffering (ping-pong). Kept in one place so
    /// no format literal or buffering assumption is duplicated anywhere else.
    /// </summary>
    internal static class WaterLayerCatalog
    {
        // Formats are chosen per the architecture draft (Section 4). Half-float carries
        // displacement/normals; the ripple sim needs full float for the WebGPU
        // mean-conservation fix; two-channel layers store XZ pairs.
        private const GraphicsFormat DisplacementFormat = GraphicsFormat.R16G16B16A16_SFloat;
        private const GraphicsFormat DynamicFormat = GraphicsFormat.R32G32B32A32_SFloat;
        private const GraphicsFormat SurfaceNormalFoamFormat = GraphicsFormat.R16G16B16A16_SFloat;
        private const GraphicsFormat DepthFormat = GraphicsFormat.R16G16_SFloat;
        private const GraphicsFormat FlowFormat = GraphicsFormat.R16G16_SFloat;
        private const GraphicsFormat AbsorptionFormat = GraphicsFormat.R8G8B8A8_UNorm;

        internal static GraphicsFormat FormatOf(WaterLayer layer)
        {
            switch (layer)
            {
                case WaterLayer.Displacement: return DisplacementFormat;
                case WaterLayer.Dynamic: return DynamicFormat;
                case WaterLayer.SurfaceNormalFoam: return SurfaceNormalFoamFormat;
                case WaterLayer.Depth: return DepthFormat;
                case WaterLayer.Flow: return FlowFormat;
                case WaterLayer.Absorption: return AbsorptionFormat;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layer), layer, "No GPU format registered for this water layer.");
            }
        }

        /// <summary>
        /// Only the ripple simulation carries state across frames, so it alone reads one
        /// buffer while writing another. Every other layer is recomputed in place.
        /// </summary>
        internal static bool IsDoubleBuffered(WaterLayer layer) => layer == WaterLayer.Dynamic;
    }
}

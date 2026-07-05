using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Assembles one frame of a domain's water passes. The domain's layer textures are persistent
    /// sim state living across frames, so they are imported into the graph (not graph-managed) with
    /// no clear; each provider then records against a frame view, and the double-buffered layers are
    /// ping-ponged once at the end. Holds no water logic itself — providers own the passes, this
    /// only imports, sequences, and swaps.
    /// </summary>
    internal sealed class WaterRenderGraphController
    {
        internal void Record(RenderGraph renderGraph, WaterDomain domain)
        {
            var field = domain.Field;
            var descriptor = field.Descriptor;

            var handles = ImportLayers(renderGraph, field, descriptor);
            var frame = new WaterFieldFrame(
                renderGraph, handles, descriptor.Resolution, descriptor.CascadeCount);

            foreach (var provider in domain.Providers)
            {
                provider.RecordPasses(frame);
            }

            SwapDoubleBufferedLayers(field, descriptor);
        }

        // A single-buffered layer reads and writes one texture, so it is imported once and the same
        // handle serves both roles; a double-buffered layer imports its distinct read and write.
        private static IReadOnlyDictionary<WaterLayer, WaterFieldFrame.LayerHandles> ImportLayers(
            RenderGraph renderGraph, WaterField field, WaterFieldDescriptor descriptor)
        {
            var handles = new Dictionary<WaterLayer, WaterFieldFrame.LayerHandles>(descriptor.Layers.Count);
            foreach (var layer in descriptor.Layers)
            {
                var read = renderGraph.ImportTexture(field.ReadHandle(layer));
                var write = WaterLayerCatalog.IsDoubleBuffered(layer)
                    ? renderGraph.ImportTexture(field.WriteHandle(layer))
                    : read;

                handles[layer] = new WaterFieldFrame.LayerHandles(read, write);
            }

            return handles;
        }

        private static void SwapDoubleBufferedLayers(WaterField field, WaterFieldDescriptor descriptor)
        {
            foreach (var layer in descriptor.Layers)
            {
                if (WaterLayerCatalog.IsDoubleBuffered(layer))
                {
                    field.Swap(layer);
                }
            }
        }
    }
}

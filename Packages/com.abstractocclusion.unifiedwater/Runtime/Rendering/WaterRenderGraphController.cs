using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Assembles one frame of a domain's water passes. The domain's layer textures are persistent sim
    /// state living across frames, so they are imported into the graph (not graph-managed) with no
    /// clear, exactly once per frame. It then runs the frame's banked fixed substeps — each substep
    /// hands providers a view whose read/write handles alternate over the two imported buffers — and
    /// finally reconciles the field's ping-pong flip so the next frame and the debug view read the
    /// latest state. Holds no water logic; providers own the passes, this imports, sequences, swaps.
    /// </summary>
    internal sealed class WaterRenderGraphController
    {
        internal void Record(RenderGraph renderGraph, WaterDomain domain)
        {
            int substeps = domain.ConsumeSubsteps();
            if (substeps <= 0)
            {
                return;
            }

            var field = domain.Field;
            var descriptor = field.Descriptor;

            var handles = ImportBaseHandles(renderGraph, field, descriptor);

            for (int substep = 0; substep < substeps; substep++)
            {
                var frame = new WaterFieldFrame(
                    renderGraph, handles, descriptor.Resolution, descriptor.CascadeCount, substep, substeps);

                foreach (var provider in domain.Providers)
                {
                    provider.RecordPasses(frame);
                }
            }

            ReconcileSwaps(field, descriptor, substeps);
        }

        // Imports each layer's buffers once. A single-buffered layer reads and writes one texture, so
        // it is imported once and both roles resolve to it; a double-buffered layer imports its two.
        private static IReadOnlyDictionary<WaterLayer, WaterFieldFrame.BaseHandles> ImportBaseHandles(
            RenderGraph renderGraph, WaterField field, WaterFieldDescriptor descriptor)
        {
            var handles = new Dictionary<WaterLayer, WaterFieldFrame.BaseHandles>(descriptor.Layers.Count);
            foreach (var layer in descriptor.Layers)
            {
                bool doubleBuffered = WaterLayerCatalog.IsDoubleBuffered(layer);
                var frameStartRead = renderGraph.ImportTexture(field.ReadHandle(layer));
                var frameStartWrite = doubleBuffered
                    ? renderGraph.ImportTexture(field.WriteHandle(layer))
                    : frameStartRead;

                handles[layer] = new WaterFieldFrame.BaseHandles(frameStartRead, frameStartWrite, doubleBuffered);
            }

            return handles;
        }

        // Each substep advanced the ping-pong once logically; applying that many CPU-side swaps leaves
        // the field's read/write orientation matching the latest written buffer for the next frame.
        private static void ReconcileSwaps(WaterField field, WaterFieldDescriptor descriptor, int substeps)
        {
            foreach (var layer in descriptor.Layers)
            {
                if (!WaterLayerCatalog.IsDoubleBuffered(layer))
                {
                    continue;
                }

                for (int i = 0; i < substeps; i++)
                {
                    field.Swap(layer);
                }
            }
        }
    }
}

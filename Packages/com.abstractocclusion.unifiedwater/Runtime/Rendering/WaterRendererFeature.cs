using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Registers the water render-graph passes with URP — add it to the renderer's feature list.
    /// It owns the shared passes: an always-on update pass that advances every registered domain's
    /// field, and an optional debug pass that blits a layer to the screen. The feature holds no
    /// water logic; it only wires the passes into the pipeline.
    /// </summary>
    public sealed class WaterRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Blit the first water domain's Dynamic layer to the screen for inspection.")]
        [SerializeField]
        private bool showDebugView;

        [Tooltip("Scales field values into the 0..1 greyscale range of the debug view.")]
        [SerializeField]
        [Min(WaterDebugConstants.MinValueScale)]
        private float debugValueScale = WaterDebugConstants.DefaultValueScale;

        private WaterFieldUpdatePass _updatePass;
        private WaterFieldDebugPass _debugPass;

        public override void Create()
        {
            _updatePass = new WaterFieldUpdatePass();
            _debugPass = new WaterFieldDebugPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_updatePass);

            if (showDebugView)
            {
                _debugPass.Configure(debugValueScale);
                renderer.EnqueuePass(_debugPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            _debugPass?.Dispose();
        }
    }
}

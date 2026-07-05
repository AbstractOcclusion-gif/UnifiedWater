using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace AbstractOcclusion.UnifiedWater
{
    /// <summary>
    /// Advances every registered water domain's field once per frame, before opaques, so the field
    /// is current when anything samples it. RecordRenderGraph runs once per camera, but the field is
    /// persistent sim state, so only the primary game camera steps it; other cameras (scene view,
    /// preview, reflection) skip stepping and sample the state as it stands. This keeps the state
    /// advancing exactly once per frame rather than once per camera.
    /// </summary>
    internal sealed class WaterFieldUpdatePass : ScriptableRenderPass
    {
        private readonly WaterRenderGraphController _controller = new WaterRenderGraphController();

        internal WaterFieldUpdatePass()
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.cameraType != CameraType.Game)
            {
                return;
            }

            var domains = WaterDomainRegistry.Domains;
            for (int i = 0; i < domains.Count; i++)
            {
                _controller.Record(renderGraph, domains[i]);
            }
        }
    }
}

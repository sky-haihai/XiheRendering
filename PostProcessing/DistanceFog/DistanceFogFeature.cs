using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.DistanceFog {
    public class DistanceFogFeature : ScriptableRendererFeature {
        [SerializeField]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public bool renderSceneView = false;

        private DistanceFogRenderPass m_RenderPass;

        public override void Create() {
            m_RenderPass = new DistanceFogRenderPass(renderPassEvent, renderSceneView);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }
}
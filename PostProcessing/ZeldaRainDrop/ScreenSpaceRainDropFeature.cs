using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.ZeldaRainDrop {
    public class ScreenSpaceRainDropFeature : ScriptableRendererFeature {
        [SerializeField]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public bool renderSceneView = false;

        private RainDropRenderPass m_RenderPass;

        public override void Create() {
            m_RenderPass = new RainDropRenderPass(renderPassEvent, renderSceneView);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }
}
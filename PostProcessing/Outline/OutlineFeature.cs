using UnityEngine;
using UnityEngine.Rendering.Universal;
using XiheRendering.PostProcessing.DistanceFog;

namespace XiheRendering.PostProcessing.Outline {
    public class OutlineFeature : ScriptableRendererFeature {
        [SerializeField]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        public bool renderSceneView = false;

        private OutlineRenderPass m_RenderPass;

        public override void Create() {
            m_RenderPass = new OutlineRenderPass(renderPassEvent, renderSceneView);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }
}
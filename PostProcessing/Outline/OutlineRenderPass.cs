using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.Outline {
    public class OutlineRenderPass : ScriptableRenderPass {
        private bool m_RenderSceneView;
        private RenderTargetHandle m_ResultTex; //camera color
        private Material m_OutlineMaterial;

        private readonly int m_ColorID = Shader.PropertyToID("_Color");
        private readonly int m_Thickness = Shader.PropertyToID("_Thickness");
        private readonly int m_Threshold = Shader.PropertyToID("_Threshold");

        public OutlineRenderPass(RenderPassEvent renderPassEvent, bool renderSceneView = false) {
            this.renderPassEvent = renderPassEvent;
            m_RenderSceneView = renderSceneView;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_ResultTex.id, descriptor);
            if (m_OutlineMaterial == null) {
                m_OutlineMaterial = new Material(Shader.Find("Hidden/XiheRendering/Outline"));
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_RenderSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "SS Outline");
            cmd.Clear();

            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<Outline>();
            // Only process if the effect is active
            if (customEffect.IsActive()) {
                if (customEffect.tintColor.overrideState) {
                    m_OutlineMaterial.SetColor(m_ColorID, customEffect.tintColor.value);
                }

                if (customEffect.thickness.overrideState) {
                    m_OutlineMaterial.SetFloat(m_Thickness, customEffect.thickness.value);
                }

                if (customEffect.threshold.overrideState) {
                    m_OutlineMaterial.SetFloat(m_Threshold, customEffect.threshold.value);
                }

                if (customEffect.debugMode.overrideState) {
                    if (customEffect.debugMode.value) {
                        m_OutlineMaterial.EnableKeyword("_DEBUG_ON");
                    }
                    else {
                        m_OutlineMaterial.DisableKeyword("_DEBUG_ON");
                    }
                }

                //cache color target
                var cam = renderingData.cameraData.renderer;
                cmd.Blit(cam.cameraColorTarget, m_ResultTex.Identifier(), m_OutlineMaterial);
                cmd.Blit(m_ResultTex.Identifier(), cam.cameraColorTarget);
                context.ExecuteCommandBuffer(cmd);
            }

            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(m_ResultTex.id);
        }
    }
}
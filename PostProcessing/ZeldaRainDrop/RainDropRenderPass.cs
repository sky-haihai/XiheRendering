using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.ZeldaRainDrop {
    public class RainDropRenderPass : ScriptableRenderPass {
        private bool m_RenderSceneView;
        private RenderTargetHandle m_ResultTex; //camera color
        private Material m_RainDropMaterial;

        private readonly int m_IntensityID = Shader.PropertyToID("_Intensity");
        private readonly int m_NoiseTexID = Shader.PropertyToID("_NoiseTex");
        private readonly int m_ColorID = Shader.PropertyToID("_Color");
        private readonly int m_NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private readonly int m_ThicknessID = Shader.PropertyToID("_Thickness");
        private readonly int m_DropSpeedID = Shader.PropertyToID("_DropSpeed");
        private readonly int m_NoiseThresholdID = Shader.PropertyToID("_NoiseThreshold");
        private readonly int m_EdgeThresholdID = Shader.PropertyToID("_EdgeThreshold");
        private readonly int m_AngleThresholdID = Shader.PropertyToID("_AngleThreshold");

        public RainDropRenderPass(RenderPassEvent renderPassEvent, bool renderSceneView=false) {
            this.renderPassEvent = renderPassEvent;
            m_RenderSceneView = renderSceneView;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_ResultTex.id, descriptor);
            if (m_RainDropMaterial == null) {
                m_RainDropMaterial = new Material(Shader.Find("Hidden/XiheRendering/ScreenSpaceRainDrop"));
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_RenderSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "Zelda Rain Drop");
            cmd.Clear();

            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<ScreenSpaceRainDrop>();
            // Only process if the effect is active
            if (customEffect.IsActive()) {
                // P.s. optimize by caching the property ID somewhere else
                m_RainDropMaterial.SetFloat(m_IntensityID, customEffect.intensity.value);
                m_RainDropMaterial.SetTexture(m_NoiseTexID, customEffect.noiseTexture.value);
                m_RainDropMaterial.SetColor(m_ColorID, customEffect.dropletColor.value);
                m_RainDropMaterial.SetFloat(m_NoiseScaleID, 1f / customEffect.dropletSize.value);
                m_RainDropMaterial.SetFloat(m_ThicknessID, customEffect.dropletThickness.value);
                m_RainDropMaterial.SetFloat(m_DropSpeedID, customEffect.dropSpeed.value);
                m_RainDropMaterial.SetFloat(m_NoiseThresholdID, customEffect.noiseThreshold.value);
                m_RainDropMaterial.SetFloat(m_EdgeThresholdID, customEffect.edgeThreshold.value);
                m_RainDropMaterial.SetFloat(m_AngleThresholdID, customEffect.angleThreshold.value);
                if (customEffect.debugMode.value) {
                    m_RainDropMaterial.EnableKeyword("_DEBUG_ON");
                }
                else {
                    m_RainDropMaterial.DisableKeyword("_DEBUG_ON");
                }

                //cache color target
                var cam = renderingData.cameraData.renderer;
                cmd.Blit(cam.cameraColorTarget, m_ResultTex.Identifier(), m_RainDropMaterial);
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
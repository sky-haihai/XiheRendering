using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.DistanceFog {
    public class DistanceFogRenderPass : ScriptableRenderPass {
        private bool m_RenderSceneView;
        private RenderTargetHandle m_ResultTex; //camera color
        private Material m_DistanceFogMaterial;

        private readonly int m_IntensityID = Shader.PropertyToID("_Intensity");
        private readonly int m_NoiseTexID = Shader.PropertyToID("_NoiseTex");
        private readonly int m_ColorID = Shader.PropertyToID("_Color");
        private readonly int m_NoiseScaleID = Shader.PropertyToID("_NoiseScale");
        private readonly int m_Exponent = Shader.PropertyToID("_Exponent");
        private readonly int m_NoiseSpeed = Shader.PropertyToID("_NoiseSpeed");
        private readonly int m_DistanceOffsetID = Shader.PropertyToID("_DistanceOffset");

        public DistanceFogRenderPass(RenderPassEvent renderPassEvent, bool renderSceneView = false) {
            this.renderPassEvent = renderPassEvent;
            m_RenderSceneView = renderSceneView;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_ResultTex.id, descriptor);
            if (m_DistanceFogMaterial == null) {
                m_DistanceFogMaterial = new Material(Shader.Find("Hidden/XiheRendering/DistanceFog"));
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_RenderSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "SS Distance Fog");
            cmd.Clear();

            var stack = VolumeManager.instance.stack;
            var customEffect = stack.GetComponent<DistanceFog>();
            // Only process if the effect is active
            if (customEffect.IsActive()) {
                // P.s. optimize by caching the property ID somewhere else
                if (customEffect.intensity.overrideState) {
                    m_DistanceFogMaterial.SetFloat(m_IntensityID, customEffect.intensity.value);
                }
                
                if (customEffect.exponent.overrideState) {
                    m_DistanceFogMaterial.SetFloat(m_Exponent, customEffect.exponent.value);
                }
                
                if (customEffect.distanceOffset.overrideState) {
                    m_DistanceFogMaterial.SetFloat(m_DistanceOffsetID, customEffect.distanceOffset.value);
                }
                
                if (customEffect.noiseTexture.overrideState) {
                    m_DistanceFogMaterial.SetTexture(m_NoiseTexID, customEffect.noiseTexture.value);
                }

                if (customEffect.tintColor.overrideState) {
                    m_DistanceFogMaterial.SetColor(m_ColorID, customEffect.tintColor.value);
                }

                if (customEffect.noiseScale.overrideState) {
                    m_DistanceFogMaterial.SetFloat(m_NoiseScaleID, customEffect.noiseScale.value);
                }

                if (customEffect.noiseSpeed.overrideState) {
                    m_DistanceFogMaterial.SetFloat(m_NoiseSpeed, customEffect.noiseSpeed.value);
                }

                if (customEffect.debugMode.overrideState) {
                    if (customEffect.debugMode.value) {
                        m_DistanceFogMaterial.EnableKeyword("_DEBUG_ON");
                    }
                    else {
                        m_DistanceFogMaterial.DisableKeyword("_DEBUG_ON");
                    }
                }

                //cache color target
                var cam = renderingData.cameraData.renderer;
                cmd.Blit(cam.cameraColorTarget, m_ResultTex.Identifier(), m_DistanceFogMaterial);
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
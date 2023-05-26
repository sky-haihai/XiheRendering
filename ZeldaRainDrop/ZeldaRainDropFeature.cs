using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

public class ZeldaRainDropFeature : ScriptableRendererFeature {
    [SerializeField] public Settings settings = new Settings();

    private RainDropRenderPass m_RenderPass;

    public override void Create() {
        m_RenderPass = new RainDropRenderPass(settings) {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (settings.rainDropShader != null && settings.rainDropShader != null) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    private class RainDropRenderPass : ScriptableRenderPass {
        private Settings m_Settings;
        private RenderTargetHandle m_ResultTex; //camera color

        public RainDropRenderPass(Settings settings) {
            m_Settings = settings;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.colorFormat = RenderTextureFormat.ARGB32;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_ResultTex.id, descriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_Settings.previewInSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "Screen Door Transparency");
            cmd.Clear();

            //cache color target
            var cam = renderingData.cameraData.renderer;

            var shader = m_Settings.rainDropShader;
            var mainKernel = shader.FindKernel("ScreenSpaceRainDrop");

            //sobel
            cmd.SetComputeTextureParam(shader, mainKernel, "_InputColorTex", cam.cameraColorTarget);
            cmd.SetComputeTextureParam(shader, mainKernel, "_InputDepthTex", cam.cameraDepthTarget);
            cmd.SetComputeTextureParam(shader, mainKernel, "_NoiseTex", m_Settings.noiseTex);
            cmd.SetComputeIntParam(shader, "_Thickness", m_Settings.thickness);
            cmd.SetComputeFloatParam(shader, "_EdgeThreshold", m_Settings.sobelThreshold);

            //noise
            cmd.SetComputeFloatParam(shader, "_RainDropScale", m_Settings.rainDropScale);
            cmd.SetComputeIntParam(shader, "_NoiseWidth", m_Settings.noiseTex.width);
            cmd.SetComputeIntParam(shader, "_NoiseHeight", m_Settings.noiseTex.height);
            cmd.SetComputeVectorParam(shader, "_Time", Shader.GetGlobalVector("_Time"));
            cmd.SetComputeFloatParam(shader, "_DropSpeed", m_Settings.dropSpeed);
            cmd.SetComputeVectorParam(shader, "_DropColor", m_Settings.dropColor);

            //output
            cmd.SetComputeTextureParam(shader, mainKernel, "_OutputTex", m_ResultTex.Identifier());
            cmd.SetComputeIntParam(shader, "_Width", renderingData.cameraData.camera.scaledPixelWidth);
            cmd.SetComputeIntParam(shader, "_Height", renderingData.cameraData.camera.scaledPixelHeight);

            int threadGroupX = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelWidth / 8.0f);
            int threadGroupY = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelHeight / 8.0f);
            cmd.DispatchCompute(shader, mainKernel, threadGroupX, threadGroupY, 1);

            cmd.Blit(m_ResultTex.id, cam.cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);

            CommandBufferPool.Release(cmd);

            context.Submit();
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(m_ResultTex.id);
        }
    }

    [System.Serializable]
    public class Settings {
        public ComputeShader rainDropShader;
        public Texture2D noiseTex;
        public Color dropColor = Color.white;
        [Range(0, 20)] public int thickness = 3;
        [Range(0f, 1f)] public float sobelThreshold = 0.166f;
        [Range(0f, 1f)] public float rainDropScale = 0.5f;
        public float dropSpeed = 100f;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public bool previewInSceneView = true;
    }
}
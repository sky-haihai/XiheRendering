using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ScreenDoorRenderFeature : ScriptableRendererFeature {
    [SerializeField] public Settings settings = new Settings();

    ScreenDoorRenderPass m_RenderPass;

    public override void Create() {
        m_RenderPass = new ScreenDoorRenderPass(settings) {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (settings.computeShader != null && settings.targetLayer != 0 && settings.obstacleLayer != 0) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    private class ScreenDoorRenderPass : ScriptableRenderPass {
        private Settings m_Settings;
        private RenderTargetHandle m_TempCameraOpaque; //camera color
        private RenderTexture m_TargetOpaqueTexture; //target layer color
        private RenderTexture m_TargetDepthTexture; //target layer depth
        private RenderTexture m_ObstacleDepthTexture; //obstacle layer depth

        public ScreenDoorRenderPass(Settings settings) {
            m_Settings = settings;
            m_TempCameraOpaque.Init("_TempCameraOpaque");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_TempCameraOpaque.id, descriptor);

            //create depth textures (reduce rt size for performance)
            m_TargetOpaqueTexture = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, RenderTextureFormat.ARGB32);
            m_TargetDepthTexture = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 16, RenderTextureFormat.Depth);
            m_ObstacleDepthTexture = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 16, RenderTextureFormat.Depth);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_Settings.previewInSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get(name: "Screen Door Transparency");
            cmd.Clear();

            // Prepare drawing settings & filters

            var drawOpaqueSettings = CreateDrawingSettings(new ShaderTagId("UniversalForward"), ref renderingData, SortingCriteria.BackToFront);
            var drawDepthSettings = CreateDrawingSettings(new ShaderTagId("DepthOnly"), ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            var targetFilter = new FilteringSettings(RenderQueueRange.all, m_Settings.targetLayer.value);
            var obstacleFilter = new FilteringSettings(RenderQueueRange.all, m_Settings.obstacleLayer.value);

            //draw target opaque
            cmd.SetRenderTarget(m_TargetOpaqueTexture);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawOpaqueSettings, ref targetFilter);

            //draw target depth
            cmd.SetRenderTarget(m_TargetDepthTexture);
            cmd.ClearRenderTarget(true, false, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawDepthSettings, ref targetFilter);

            //draw obstacle depth
            cmd.SetRenderTarget(m_ObstacleDepthTexture);
            cmd.ClearRenderTarget(true, false, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawDepthSettings, ref obstacleFilter);

            var cam = renderingData.cameraData.renderer;
            cmd.SetRenderTarget(cam.cameraColorTarget, cam.cameraDepthTarget);
            //cache input color
            cmd.Blit(cam.cameraColorTarget, m_TempCameraOpaque.Identifier());

            var shader = m_Settings.computeShader;
            var mainKernel = shader.FindKernel("BayerOrderedDithering");

            //set parameters
            cmd.SetComputeTextureParam(shader, mainKernel, "_InputTexture", cam.cameraColorTarget);
            cmd.SetComputeTextureParam(shader, mainKernel, "_TargetOpaqueTexture", m_TargetOpaqueTexture);
            cmd.SetComputeTextureParam(shader, mainKernel, "_TargetDepthTexture", m_TargetDepthTexture);
            cmd.SetComputeTextureParam(shader, mainKernel, "_ObstacleDepthTexture", m_ObstacleDepthTexture);
            cmd.SetComputeTextureParam(shader, mainKernel, "_OutputTexture", m_TempCameraOpaque.Identifier());
            cmd.SetComputeFloatParam(shader, "_DitherThreshold", m_Settings.ditheringThreshold);

            int threadGroupX = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelWidth / 8.0f);
            int threadGroupY = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelHeight / 8.0f);
            cmd.DispatchCompute(shader, mainKernel, threadGroupX, threadGroupY, 1);

            cmd.Blit(m_TempCameraOpaque.id, cam.cameraColorTarget);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            context.Submit();
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(m_TempCameraOpaque.id);
            RenderTexture.ReleaseTemporary(m_TargetOpaqueTexture);
            RenderTexture.ReleaseTemporary(m_TargetDepthTexture);
            RenderTexture.ReleaseTemporary(m_ObstacleDepthTexture);
        }
    }

    [System.Serializable]
    public class Settings {
        public ComputeShader computeShader;
        public LayerMask targetLayer;
        public LayerMask obstacleLayer;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public bool previewInSceneView = true;

        [Range(0, 1f)] public float ditheringThreshold = 0.5f;
    }
}
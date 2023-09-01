using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XihePostProcessing.ObjectSpacePixelation {
    public class UsePixelationMapPass : ScriptableRenderPass {
        private ObjectSpacePixelationFeature.Settings m_Settings;
        private ShaderTagId m_ObjectSpacePixelationShaderTag = new ShaderTagId("ObjectSpacePixelation");

        private RenderTexture m_SourceColorRT;
        private RenderTexture m_SourceDepthRT;

        private RTHandle m_OutputRT;

        public UsePixelationMapPass(ObjectSpacePixelationFeature.Settings settings) {
            m_Settings = settings;
            m_OutputRT = RTHandles.Alloc("_OutputRT", name: "_OutputRT");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.enableRandomWrite = true;
            cmd.GetTemporaryRT(Shader.PropertyToID(m_OutputRT.name), descriptor);

            m_SourceColorRT = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 0, RenderTextureFormat.ARGB32);
            m_SourceDepthRT = RenderTexture.GetTemporary(cameraTextureDescriptor.width, cameraTextureDescriptor.height, 32, RenderTextureFormat.Depth);
            m_SourceColorRT.filterMode = FilterMode.Point;
            m_SourceDepthRT.filterMode = FilterMode.Point;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            CommandBuffer cmd = CommandBufferPool.Get("Object Space Pixelation");
            cmd.Clear();

            var drawDitherSetting = CreateDrawingSettings(m_ObjectSpacePixelationShaderTag, ref renderingData, SortingCriteria.BackToFront);
            var drawDitherFilter = new FilteringSettings(RenderQueueRange.all);

            //draw target opaque
            cmd.SetRenderTarget(m_SourceColorRT, m_SourceDepthRT);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawDitherSetting, ref drawDitherFilter);

            var shader = m_Settings.usePixelationShader;
            var mainKernel = shader.FindKernel("FillDitherTexture");

            //set parameters
            cmd.SetComputeTextureParam(shader, mainKernel, "_CameraOpaqueTexture", renderingData.cameraData.renderer.cameraColorTargetHandle);
            cmd.SetComputeTextureParam(shader, mainKernel, "_CameraDepthTexture", renderingData.cameraData.renderer.cameraDepthTargetHandle);
            cmd.SetComputeTextureParam(shader, mainKernel, "_ObjectColorTexture", m_SourceColorRT);
            cmd.SetComputeTextureParam(shader, mainKernel, "_ObjectDepthTexture", m_SourceDepthRT);
            cmd.SetComputeFloatParam(shader, "_CameraNear", renderingData.cameraData.camera.nearClipPlane);
            cmd.SetComputeFloatParam(shader, "_CameraFar", renderingData.cameraData.camera.farClipPlane);
            cmd.SetComputeTextureParam(shader, mainKernel, "_OutputTexture", m_OutputRT);

            int threadGroupX = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelWidth / 8.0f);
            int threadGroupY = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelHeight / 8.0f);
            cmd.DispatchCompute(shader, mainKernel, threadGroupX, threadGroupY, 1);

            cmd.Blit(m_OutputRT.nameID, renderingData.cameraData.renderer.cameraColorTargetHandle);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            RenderTexture.ReleaseTemporary(m_SourceColorRT);
            RenderTexture.ReleaseTemporary(m_SourceDepthRT);
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_OutputRT.name));
        }
    }
}
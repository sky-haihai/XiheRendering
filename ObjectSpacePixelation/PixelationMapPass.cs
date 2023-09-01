using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XihePostProcessing.ObjectSpacePixelation {
    public class PixelationMapPass : ScriptableRenderPass {
        private ObjectSpacePixelationFeature.Settings m_Settings;
        private ShaderTagId m_ObjectSpacePixelationShaderTag = new ShaderTagId("ObjectSpacePixelation");

        private RTHandle m_SourceColorRT;
        private RTHandle m_SourceDepthRT;
        private RTHandle m_OutputRT;

        public PixelationMapPass(ObjectSpacePixelationFeature.Settings settings) {
            m_Settings = settings;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var descriptor = cameraTextureDescriptor;
            descriptor.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(ref m_SourceColorRT, descriptor, filterMode: FilterMode.Point, name: "_SourceColorRT");

            descriptor.enableRandomWrite = true;
            RenderingUtils.ReAllocateIfNeeded(ref m_OutputRT, descriptor, filterMode: FilterMode.Point, name: "_OutputRT");

            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 16;
            descriptor.enableRandomWrite = false;
            RenderingUtils.ReAllocateIfNeeded(ref m_SourceDepthRT, descriptor, filterMode: FilterMode.Point, name: "_SourceDepthRT");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (renderingData.cameraData.cameraType == CameraType.SceneView && !m_Settings.previewInSceneView) {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("Object Space Pixelation");
            cmd.Clear();

            var drawObjSettings = CreateDrawingSettings(m_ObjectSpacePixelationShaderTag, ref renderingData, SortingCriteria.BackToFront);
            var drawObjFilter = new FilteringSettings(RenderQueueRange.opaque);

            //draw target opaque
            cmd.SetRenderTarget(m_SourceColorRT, m_SourceDepthRT);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            context.DrawRenderers(renderingData.cullResults, ref drawObjSettings, ref drawObjFilter);

            var shader = m_Settings.drawPixelationMapShader;
            var mainKernel = shader.FindKernel("DrawPixelationMap");

            //set parameters
            cmd.SetComputeTextureParam(shader, mainKernel, "_OutputTexture", m_OutputRT.nameID);
            cmd.SetComputeTextureParam(shader, mainKernel, "_ObjectColorTexture", m_SourceColorRT.nameID);
            cmd.SetComputeTextureParam(shader, mainKernel, "_ObjectDepthTexture", m_SourceDepthRT.nameID);
            cmd.SetComputeFloatParam(shader, "_ScreenTexelSizeX", renderingData.cameraData.cameraTargetDescriptor.width / (float)renderingData.cameraData.camera.pixelWidth);
            cmd.SetComputeFloatParam(shader, "_ScreenTexelSizeY", renderingData.cameraData.cameraTargetDescriptor.height / (float)renderingData.cameraData.camera.pixelWidth);

            shader.GetKernelThreadGroupSizes(mainKernel, out uint threadGroupX, out uint threadGroupY, out _);
            int groupCountX = Mathf.RoundToInt(renderingData.cameraData.camera.scaledPixelWidth / (float)threadGroupX);
            int groupCountY = Mathf.RoundToInt(renderingData.cameraData.camera.scaledPixelHeight / (float)threadGroupY);
            cmd.DispatchCompute(shader, mainKernel, groupCountX, groupCountY, 1);

            cmd.Blit(m_OutputRT.nameID, renderingData.cameraData.renderer.cameraColorTargetHandle);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            context.Submit();
        }

        public void Dispose() {
            m_SourceColorRT?.Release();
            m_OutputRT?.Release();
        }
    }
}
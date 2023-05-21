using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PalettePixelationRenderFeature : ScriptableRendererFeature {
    [SerializeField] public Settings settings = new Settings();

    private PalettePixelationRenderPass m_RenderPass;

    public override void Create() {
        m_RenderPass = new PalettePixelationRenderPass(settings) {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (settings.computeShader != null && settings.paletteData != null) {
            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public class PalettePixelationRenderPass : ScriptableRenderPass {
        private Settings m_Settings;
        private RenderTargetHandle m_OutputRT; //output color
        private ComputeBuffer m_PaletteBuffer; //palette buffer
        private int m_PaletteLength; //palette length

        public PalettePixelationRenderPass(Settings settings) {
            m_Settings = settings;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var desc = cameraTextureDescriptor;
            desc.enableRandomWrite = true;
            cmd.GetTemporaryRT(m_OutputRT.id, desc);

            var colorArray = m_Settings.paletteData.paletteColors.ToArray();
            m_PaletteBuffer = new ComputeBuffer(colorArray.Length, sizeof(float) * 4);
            m_PaletteBuffer.SetData(colorArray);
            m_PaletteLength = colorArray.Length;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
            if (!m_Settings.previewInSceneView && (renderingData.cameraData.isSceneViewCamera || renderingData.cameraData.isPreviewCamera)) {
                return;
            }

            var cmd = CommandBufferPool.Get("Palette Pixelation");
            cmd.Clear();

            var renderer = renderingData.cameraData.renderer;

            //compute shader
            var shader = m_Settings.computeShader;
            var kernel = shader.FindKernel("PalettePixelate");
            cmd.SetComputeTextureParam(shader, kernel, "_MainTex", renderer.cameraColorTarget);
            cmd.SetComputeTextureParam(shader, kernel, "_ResultTex", m_OutputRT.Identifier());
            cmd.SetComputeFloatParam(shader, "PixelSize", m_Settings.pixelSize);
            cmd.SetComputeBufferParam(shader, kernel, "_PaletteBuffer", m_PaletteBuffer);
            cmd.SetComputeIntParam(shader, "_PaletteLength", m_PaletteLength);

            shader.GetKernelThreadGroupSizes(kernel, out uint x, out uint y, out uint z);

            int threadGroupX = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelWidth / (float)x);
            int threadGroupY = Mathf.CeilToInt(renderingData.cameraData.camera.scaledPixelHeight / (float)y);
            cmd.DispatchCompute(shader, kernel, threadGroupX, threadGroupY, 1);

            cmd.Blit(m_OutputRT.id, renderer.cameraColorTarget);
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
        }

        public override void FrameCleanup(CommandBuffer cmd) {
            cmd.ReleaseTemporaryRT(m_OutputRT.id);
            m_PaletteBuffer.Dispose();
        }
    }

    [Serializable]
    public class Settings {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        public ComputeShader computeShader;
        public PaletteData paletteData;
        public float pixelSize = 1f;

        public bool previewInSceneView = true;
    }
}
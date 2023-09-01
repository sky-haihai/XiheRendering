using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace XihePostProcessing.ObjectSpacePixelation {
    public class ObjectSpacePixelationFeature : ScriptableRendererFeature {
        [SerializeField]
        public Settings settings = new Settings();

        PixelationMapPass m_PixelationMapPass;
        UsePixelationMapPass m_UsePixelationMapPass;

        public override void Create() {
            m_PixelationMapPass = new PixelationMapPass(settings) {
                renderPassEvent = settings.renderPassEvent
            };

            m_UsePixelationMapPass = new UsePixelationMapPass(settings) {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (settings.drawPixelationMapShader == null) return;
            renderer.EnqueuePass(m_PixelationMapPass);

            if (settings.usePixelationShader == null) return;
            renderer.EnqueuePass(m_UsePixelationMapPass);
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                m_PixelationMapPass.Dispose();
            }
        }

        [Serializable]
        public class Settings {
            public ComputeShader drawPixelationMapShader;
            public ComputeShader usePixelationShader;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public bool previewInSceneView = true;
        }
    }
}
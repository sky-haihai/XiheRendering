using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace XihePostProcessing.ObjectSpacePixelation {
    public class ObjectSpacePixelationFeature : ScriptableRendererFeature {
        [SerializeField]
        public Settings settings = new Settings();

        ObjectSpacePixelationPass m_ObjectSpacePixelationPass;

        public override void Create() {
            m_ObjectSpacePixelationPass = new ObjectSpacePixelationPass(settings) {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (settings.computeShader == null) return;
            
            renderer.EnqueuePass(m_ObjectSpacePixelationPass);
        }

        [Serializable]
        public class Settings {
            public ComputeShader computeShader;
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            public bool previewInSceneView = true;
        }
    }
}
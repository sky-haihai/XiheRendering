using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.ZeldaRainDrop {
    [System.Serializable, VolumeComponentMenuForRenderPipeline("XiheRendering/Rain Droplet", typeof(UniversalRenderPipeline))]
    public class ScreenSpaceRainDrop : VolumeComponent, IPostProcessComponent {
        public BoolParameter enabled = new BoolParameter(true, true);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f, true);
        public NoInterpTextureParameter noiseTexture = new NoInterpTextureParameter(null, true);
        public ColorParameter dropletColor = new ColorParameter(new Color(0.3f, 0.3f, 0.3f, 1f), true);
        public ClampedFloatParameter dropletSize = new ClampedFloatParameter(0.2f, 0f, 10f, true);
        public FloatParameter dropletThickness = new FloatParameter(10f, true);
        public NoInterpFloatParameter dropSpeed = new NoInterpFloatParameter(100f, true);
        public NoInterpClampedFloatParameter noiseThreshold = new NoInterpClampedFloatParameter(0.2f, 0f, 1f, true);
        public NoInterpClampedFloatParameter edgeThreshold = new NoInterpClampedFloatParameter(0.08f, 0f, 1f, true);
        public NoInterpClampedFloatParameter angleThreshold = new NoInterpClampedFloatParameter(0.6f, 0f, 1f, true);
        public BoolParameter debugMode = new BoolParameter(false);
        
        public bool IsActive() => intensity.value > 0f && enabled.value;

        public bool IsTileCompatible() => false;
    }
}
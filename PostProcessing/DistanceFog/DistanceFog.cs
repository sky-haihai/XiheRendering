using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.DistanceFog {
    [System.Serializable, VolumeComponentMenuForRenderPipeline("XiheRendering/Distance Fog", typeof(UniversalRenderPipeline))]
    public class DistanceFog : VolumeComponent, IPostProcessComponent {
        public BoolParameter enabled = new BoolParameter(true, true);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 10f, true);
        public NoInterpClampedFloatParameter exponent = new NoInterpClampedFloatParameter(0f, 0f, 10f, true);
        public NoInterpClampedFloatParameter distanceOffset = new NoInterpClampedFloatParameter(0f, -1f, 1f, true);
        public NoInterpTextureParameter noiseTexture = new NoInterpTextureParameter(null, false);
        public NoInterpClampedFloatParameter noiseScale = new NoInterpClampedFloatParameter(1f, 0f, 10f, true);
        public NoInterpClampedFloatParameter noiseSpeed = new NoInterpClampedFloatParameter(0f, -10f, 10f, true);
        public ColorParameter tintColor = new ColorParameter(new Color(0.3f, 0.3f, 0.3f, 1f), true);
        public BoolParameter debugMode = new BoolParameter(false);

        public bool IsActive() => intensity.value > 0f && enabled.value;

        public bool IsTileCompatible() => false;
    }
}
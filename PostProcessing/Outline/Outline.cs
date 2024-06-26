using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace XiheRendering.PostProcessing.Outline {
    [System.Serializable, VolumeComponentMenuForRenderPipeline("XiheRendering/Outline", typeof(UniversalRenderPipeline))]
    public class Outline : VolumeComponent, IPostProcessComponent {
        public BoolParameter enabled = new BoolParameter(true, true);
        public ColorParameter tintColor = new ColorParameter(new Color(0.3f, 0.3f, 0.3f, 1f), true);
        public FloatParameter thickness = new FloatParameter(1f, true);
        public NoInterpClampedFloatParameter threshold = new NoInterpClampedFloatParameter(0.5f, 0f, 1f, true);
        public BoolParameter debugMode = new BoolParameter(false);

        public bool IsActive() => thickness.value > 0f && enabled.value;

        public bool IsTileCompatible() => false;
    }
}
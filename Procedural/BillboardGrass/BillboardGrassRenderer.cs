using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace XiheRendering.Procedural.BillboardGrass {
    public class BillboardGrassRenderer : MonoBehaviour {
        public Mesh mesh;
        public int subMeshIndex = 0;

        // material
        public Texture2D colorMap;
        public Texture2D controlMap;
        public Texture2D noiseMap;
        public int layer;
        public ShadowCastingMode castShadows = ShadowCastingMode.On;
        public bool receiveShadows = true;

        // instancing
        public Vector3 dimension = new Vector3(10, 10, 10);
        public Vector2 density = new Vector2(10, 10);
        public float scale = 1;
        public Vector4 controlNoiseScale = new Vector4(1, 1, 1, 1);
        public Vector3 swingScale = new Vector3(1, 1, 1);
        public float scaleSwingScale = 1;
        public float swingSpeed = 1;

        public bool renderInSceneCamera;

        private Material m_Material;

        private Vector2 m_CachedDensity = Vector2.zero;
        private Vector3 m_CachedDimension = Vector3.zero;
        private Vector4 m_CachedControlNoiseScale = Vector4.zero;
        private Vector3 m_CachedSwingScale = Vector3.zero;
        private float m_CachedScaleSwingScale = 0;
        private float m_CachedSwingSpeed = 0;
        private Texture2D m_CachedColorMap;
        private Texture2D m_CachedControlMap;
        private Texture2D m_CachedNoiseMap;

        private ComputeBuffer m_ArgsBuffer;
        private readonly uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };
        private MaterialPropertyBlock m_PropertyBlock;
        private static readonly int DimensionAndDensityPropertyID = Shader.PropertyToID("_Dimension");
        private static readonly int DimensionYPropertyID = Shader.PropertyToID("_DimensionY");
        private static readonly int ColorMapPropertyID = Shader.PropertyToID("_MainTex");
        private static readonly int ControlMapPropertyID = Shader.PropertyToID("_ControlTex");
        private static readonly int NoiseMapPropertyID = Shader.PropertyToID("_NoiseTex");
        private static readonly int SwingSpeedPropertyID = Shader.PropertyToID("_Speed");
        private static readonly int ControlNoiseScale = Shader.PropertyToID("_ControlNoiseScale");
        private static readonly int SwingScale = Shader.PropertyToID("_SwingScale");
        private static readonly int Scale = Shader.PropertyToID("_Scale");

        void Start() {
            if (m_ArgsBuffer == null) {
                m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            }

            m_PropertyBlock = new MaterialPropertyBlock();
            UpdateBuffers();
        }

        private void OnEnable() {
            if (m_ArgsBuffer == null) {
                m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            }
        }

        void Update() {
            // Update starting position buffer
            if (m_CachedDensity != density || m_CachedDimension != dimension || m_CachedControlNoiseScale != controlNoiseScale || m_CachedSwingScale != swingScale ||
                Math.Abs(m_CachedScaleSwingScale - scaleSwingScale) > float.Epsilon || colorMap != m_CachedColorMap || controlMap != m_CachedControlMap ||
                noiseMap != m_CachedNoiseMap || Math.Abs(m_CachedSwingSpeed - swingSpeed) > float.Epsilon) {
                UpdateBuffers();
            }

            // Render
            Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, m_Material, new Bounds(transform.position, dimension), m_ArgsBuffer, 0, m_PropertyBlock,
                castShadows, receiveShadows, layer, renderInSceneCamera ? null : UnityEngine.Camera.main);
        }

        void UpdateBuffers() {
            if (mesh == null) {
                m_Args[0] = m_Args[1] = m_Args[2] = m_Args[3] = 0;
                m_PropertyBlock.Clear();
                Debug.LogWarning("You forgot to assign a mesh to GPU grass generator");
                return;
            }

            // Ensure submesh index is in range
            subMeshIndex = Mathf.Clamp(subMeshIndex, 0, mesh.subMeshCount - 1);
            density = new Vector2(Mathf.Max(0, density.x), Mathf.Max(0, density.y));
            dimension = new Vector3(Mathf.Max(0, dimension.x), Mathf.Max(0, dimension.y), Mathf.Max(0, dimension.z));

            //set properties
            if (m_Material == null) {
                m_Material = new Material(Shader.Find("Hidden/XiheRendering/BillboardGrassUnlit"));
            }

            var dimensionAndDensity = new Vector4(dimension.x, dimension.z, density.x, density.y);
            m_PropertyBlock.SetVector(DimensionAndDensityPropertyID, dimensionAndDensity);
            m_PropertyBlock.SetFloat(DimensionYPropertyID, dimension.y);
            m_PropertyBlock.SetFloat(Scale, scale);
            m_PropertyBlock.SetTexture(ColorMapPropertyID, colorMap);
            m_PropertyBlock.SetTexture(ControlMapPropertyID, controlMap);
            m_PropertyBlock.SetTexture(NoiseMapPropertyID, noiseMap);
            m_PropertyBlock.SetFloat(SwingSpeedPropertyID, swingSpeed);
            m_PropertyBlock.SetVector(ControlNoiseScale, controlNoiseScale);
            m_PropertyBlock.SetVector(SwingScale, new Vector4(swingScale.x, swingScale.y, swingScale.z, scaleSwingScale));

            // Args
            // index count per instance,
            // instance count,
            // start index location,
            // base vertex location,
            // start instance location.
            m_Args[0] = (uint)mesh.GetIndexCount(subMeshIndex);
            m_Args[1] = (uint)Mathf.FloorToInt(density.x * dimension.x * density.y * dimension.z);
            m_Args[2] = (uint)mesh.GetIndexStart(subMeshIndex);
            m_Args[3] = (uint)mesh.GetBaseVertex(subMeshIndex);

            m_ArgsBuffer.SetData(m_Args);

            m_CachedDensity = density;
            m_CachedDimension = dimension;
        }

        void OnDisable() {
            if (m_ArgsBuffer != null)
                m_ArgsBuffer.Release();
            m_ArgsBuffer = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            //draw bounding box
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, dimension);
        }
#endif
    }
}
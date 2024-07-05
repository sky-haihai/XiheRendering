#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using UnityEngine;
using UnityEngine.Rendering;
using ComputeBuffer = UnityEngine.ComputeBuffer;

namespace XiheRendering.Procedural.OilPaint {
    public class BillboardStrokesRenderer : MonoBehaviour {
        public Mesh billboardMesh;
        public Material billboardMaterial;
        public MeshFilter baseMeshFilter;

        public MeshRenderer baseMeshRenderer;
        public SkinnedMeshRenderer baseSkinnedMeshRenderer;
        public bool useSkinnedMeshRenderer = false;

        // material
        public float scaleMax = 1;
        public float scaleMin = 0;

        [Range(0, 1f)]
        public float rotationRandomness = 0.2f;

        [Range(0, 1f)]
        public float bumpiness = 0.2f;

        [Range(0, 1f)]
        public float alphaCutoff = 0.2f;

        public int layer;

        public bool alwaysUpdate = false;
        public bool renderInSceneCamera = true;
        public bool enableDebug = false;

        private float m_CachedScaleMax = -1;
        private float m_CachedScaleMin = -1;
        private float m_CachedRotationRandomness = -1;
        private Vector3 m_CachedBaseMeshScale = Vector3.zero;
        private float m_CachedBumpiness = -1;
        private float m_CachedAlphaCutoff = -1;

        private Material m_OriginalMaterial;

        //stroke data
        private Mesh m_SourceMesh;
        private ComputeBuffer m_StrokeDataBuffer;
        private StrokeData[] m_StrokeDataArray;
        private StrokeData m_TempStrokeData;
        private Vector3[] m_Vertices;
        private Vector3[] m_Normals;
        private Vector4[] m_Tangents;
        private Color[] m_Colors;

        //gpu instancing
        private ComputeBuffer m_ArgsBuffer;
        private uint[] m_Args = new uint[5] { 0, 0, 0, 0, 0 };
        private MaterialPropertyBlock m_PropertyBlock;
        private Bounds m_Bounds;

        //shader properties
        private static readonly int RotationRandomness = Shader.PropertyToID("_RotationRandomness");
        private static readonly int TRSMatrix = Shader.PropertyToID("_TRSMatrix");
        private static readonly int HeightOffset = Shader.PropertyToID("_HeightOffset");
        private static readonly int AlphaCutoff = Shader.PropertyToID("_AlphaCutoff");
        private static readonly int ScaleMin = Shader.PropertyToID("_ScaleMin");
        private static readonly int ScaleMax = Shader.PropertyToID("_ScaleMax");
        private static readonly int StrokeDataBuffer = Shader.PropertyToID("_StrokeDataBuffer");

        [Serializable]
        private struct StrokeData {
            public Vector3 position;
            public Vector3 normal;
            public Vector4 tangent;
            public Vector4 color;
        }

#if UNITY_EDITOR
        private void OnValidate() {
            if (billboardMesh == null) {
                billboardMesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/XiheRendering/Procedural/OilPaint/Template/Quad.asset");
            }

            // if (billboardMaterial == null) {
            //     billboardMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/XiheRendering/Procedural/OilPaint/Template/BrushStroke.mat");
            // }

            if (baseMeshFilter == null) baseMeshFilter = GetComponent<MeshFilter>();
            if (baseMeshRenderer == null) {
                if (TryGetComponent<MeshRenderer>(out baseMeshRenderer)) {
                    baseSkinnedMeshRenderer = null;
                }
            }

            if (baseSkinnedMeshRenderer == null) {
                if (TryGetComponent<SkinnedMeshRenderer>(out baseSkinnedMeshRenderer)) {
                    baseMeshFilter = null;
                    baseMeshRenderer = null;
                }
            }
        }
#endif

        void Start() {
            Init();
        }

        void Init() {
            m_StrokeDataBuffer?.Dispose();
            m_ArgsBuffer?.Dispose();

            if (billboardMaterial == null) {
                return;
            }

            if (useSkinnedMeshRenderer) {
                m_SourceMesh = new Mesh();
                baseSkinnedMeshRenderer.BakeMesh(m_SourceMesh);
                // baseSkinnedMeshRenderer.GetVertexBuffer().GetData(m_Vertices);
            }
            else {
                m_SourceMesh = baseMeshFilter.mesh;
            }

            var vertexCount = m_SourceMesh.vertexCount;
            m_StrokeDataBuffer = new ComputeBuffer(vertexCount, sizeof(float) * (3 + 3 + 4 + 4));
            m_StrokeDataArray = new StrokeData[vertexCount];
            m_TempStrokeData = new StrokeData();
            m_Vertices = m_SourceMesh.vertices;
            m_Normals = m_SourceMesh.normals;
            m_Tangents = m_SourceMesh.tangents;
            m_Colors = m_SourceMesh.colors;

            m_ArgsBuffer = new ComputeBuffer(1, m_Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            m_OriginalMaterial = billboardMaterial;
            billboardMaterial = new Material(billboardMaterial);

            m_PropertyBlock = new MaterialPropertyBlock();
        }

        void Update() {
            if (SettingChanged() || alwaysUpdate) {
                UpdateBuffers();
            }

            m_Bounds = useSkinnedMeshRenderer ? baseSkinnedMeshRenderer.bounds : baseMeshRenderer.bounds;

            // Render
            Graphics.DrawMeshInstancedIndirect(billboardMesh, 0, billboardMaterial, m_Bounds, m_ArgsBuffer, 0, m_PropertyBlock,
                ShadowCastingMode.On, true, layer, renderInSceneCamera ? null : Camera.main);
        }

        private void OnDestroy() {
            billboardMaterial = m_OriginalMaterial;
            m_StrokeDataBuffer?.Dispose();
            m_ArgsBuffer?.Dispose();
        }

        private bool SettingChanged() {
            bool changed = false;
            if (Mathf.Abs(m_CachedScaleMax - scaleMax) > 0.01f) changed = true;
            if (Mathf.Abs(m_CachedScaleMin - scaleMin) > 0.01f) changed = true;
            if (Mathf.Abs(m_CachedRotationRandomness - rotationRandomness) > 0.01f) changed = true;
            if (Mathf.Abs(transform.localScale.magnitude - m_CachedBaseMeshScale.magnitude) > 0.01f) changed = true;
            if (Mathf.Abs(m_CachedBumpiness - bumpiness) > 0.01f) changed = true;
            if (Mathf.Abs(m_CachedAlphaCutoff - alphaCutoff) > 0.01f) changed = true;

            if (changed) {
                m_CachedScaleMax = scaleMax;
                m_CachedScaleMin = scaleMin;
                m_CachedRotationRandomness = rotationRandomness;
                m_CachedBaseMeshScale = transform.localScale;
                m_CachedBumpiness = bumpiness;
                m_CachedAlphaCutoff = alphaCutoff;
                return true;
            }

            return false;
        }

        void UpdateBuffers() {
            if (billboardMesh == null) {
                m_Args[0] = m_Args[1] = m_Args[2] = m_Args[3] = 0;
                m_PropertyBlock.Clear();
                Debug.LogWarning("You forgot to assign a mesh to BillboardStrokesRenderer");
                return;
            }

            if (useSkinnedMeshRenderer) {
                baseSkinnedMeshRenderer.BakeMesh(m_SourceMesh);
                m_Vertices = m_SourceMesh.vertices;
                m_Normals = m_SourceMesh.normals;
                m_Tangents = m_SourceMesh.tangents;
                m_Colors = m_SourceMesh.colors;
            }

            for (int i = 0; i < m_Vertices.Length; i++) {
                m_TempStrokeData.position = m_Vertices[i];
                m_TempStrokeData.normal = m_Normals[i];
                m_TempStrokeData.tangent = m_Tangents[i];
                m_TempStrokeData.color = m_Colors[i];

                m_StrokeDataArray[i] = m_TempStrokeData;
            }

            m_StrokeDataBuffer.SetData(m_StrokeDataArray);

            billboardMaterial.SetBuffer(StrokeDataBuffer, m_StrokeDataBuffer);

            m_PropertyBlock.SetFloat(RotationRandomness, rotationRandomness);
            m_PropertyBlock.SetMatrix(TRSMatrix, transform.localToWorldMatrix);
            m_PropertyBlock.SetFloat(HeightOffset, bumpiness);
            m_PropertyBlock.SetFloat(AlphaCutoff, alphaCutoff);
            scaleMin = Mathf.Clamp(scaleMin, 0, scaleMax);
            m_PropertyBlock.SetFloat(ScaleMin, scaleMin);
            m_PropertyBlock.SetFloat(ScaleMax, scaleMax);

            // Args
            // 0 index count per instance,
            // 1 instance count,
            // 2 start index location,
            // 3 base vertex location,
            // 4 start instance location.
            m_Args[0] = (uint)billboardMesh.GetIndexCount(0);
            m_Args[1] = (uint)m_SourceMesh.vertexCount;
            m_Args[2] = (uint)billboardMesh.GetIndexStart(0);
            m_Args[3] = (uint)billboardMesh.GetBaseVertex(0);

            m_ArgsBuffer.SetData(m_Args);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos() {
            //draw bounding box
            if (!enableDebug) {
                return;
            }

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(m_Bounds.center, m_Bounds.size);
        }
#endif
    }
}
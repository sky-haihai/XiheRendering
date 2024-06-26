using UnityEditor;
using UnityEngine;

namespace XiheRendering.Procedural.BillboardGrass.Editor {
    [UnityEditor.CustomEditor(typeof(BillboardGrassRenderer))]
    public class BillboardGrassRendererEditor : UnityEditor.Editor {
        private BillboardGrassRenderer m_Target;

        private void OnEnable() {
            m_Target = (BillboardGrassRenderer)target;
        }

        public override void OnInspectorGUI() {
            GUILayout.Label("Mesh Settings", EditorStyles.boldLabel);
            m_Target.mesh = EditorGUILayout.ObjectField("Mesh", m_Target.mesh, typeof(UnityEngine.Mesh), false) as UnityEngine.Mesh;
            if (m_Target.mesh != null) {
                m_Target.subMeshIndex = EditorGUILayout.IntSlider("SubMesh Index", m_Target.subMeshIndex, 0, m_Target.mesh.subMeshCount - 1);
            }
            else {
                m_Target.subMeshIndex = 0;
                EditorGUILayout.HelpBox("Mesh is not assigned", MessageType.Warning);
            }

            EditorGUILayout.Space();

            GUILayout.Label("Material Settings", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Color Map");
            m_Target.colorMap = EditorGUILayout.ObjectField(m_Target.colorMap, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Control Map");
            m_Target.controlMap =
                EditorGUILayout.ObjectField(m_Target.controlMap, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label("Noise Map");
            m_Target.noiseMap =
                EditorGUILayout.ObjectField(m_Target.noiseMap, typeof(Texture2D), false, GUILayout.Width(64), GUILayout.Height(64)) as Texture2D;
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.controlNoiseScale.x = EditorGUILayout.Slider("Position Randomness X", m_Target.controlNoiseScale.x, 0f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.controlNoiseScale.y = EditorGUILayout.Slider("Position Randomness Z", m_Target.controlNoiseScale.y, 0f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.controlNoiseScale.z = EditorGUILayout.Slider("Scale Randomness", m_Target.controlNoiseScale.z, 0f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.swingScale = EditorGUILayout.Vector3Field("Wind Scale", m_Target.swingScale);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.swingSpeed = EditorGUILayout.Slider("Wind Speed", m_Target.swingSpeed, 0f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.scaleSwingScale = EditorGUILayout.Slider("Scale Speed", m_Target.scaleSwingScale, 0f, 10f);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Target.scale = EditorGUILayout.Slider("Scale", m_Target.scale, 0f, 5f);
            GUILayout.EndHorizontal();


            EditorGUILayout.Space();

            GUILayout.Label("Render Settings", EditorStyles.boldLabel);

            m_Target.layer = EditorGUILayout.LayerField("Layer", m_Target.layer);
            m_Target.castShadows = (UnityEngine.Rendering.ShadowCastingMode)EditorGUILayout.EnumPopup("Cast Shadows", m_Target.castShadows);
            m_Target.receiveShadows = EditorGUILayout.Toggle("Receive Shadows", m_Target.receiveShadows);
            m_Target.dimension = EditorGUILayout.Vector3Field("Dimension X", m_Target.dimension);
            m_Target.density.x = EditorGUILayout.Slider("Density X", m_Target.density.x, 0.1f, 10);
            m_Target.density.y = EditorGUILayout.Slider("Density Z", m_Target.density.y, 0.1f, 10);

            EditorGUILayout.Space();
            var instanceCount = Mathf.FloorToInt(m_Target.density.x * m_Target.density.y * m_Target.dimension.x * m_Target.dimension.z);
            EditorGUILayout.LabelField($"Instance Count: {instanceCount}", EditorStyles.boldLabel);
            m_Target.renderInSceneCamera = EditorGUILayout.Toggle("Render Scene View", m_Target.renderInSceneCamera);
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace XiheRendering.Procedural.VineMaker.Editor {
    public class CubicBezierNodeEditorWindow : EditorWindow {
        private Vector3 m_KnotPos;
        private Vector3 m_LeftTangentPos;
        private Vector3 m_RightTangentPos;
        private float m_Radius;
        private float m_Tilt;

        [MenuItem("XiheRendering/Vine Curve Editor")]
        public static void ShowWindow() {
            GetWindow<CubicBezierNodeEditorWindow>("Curve Node Editor");
        }
        
        private void OnGUI() {
            #region Title

            GUILayout.Label("Curve Node Editor", EditorStyles.boldLabel);

            #endregion

            EditorGUILayout.Space();

            #region Data

            var selected = Selection.activeGameObject;
            if (selected == null) {
                return;
            }

            GUILayout.BeginHorizontal();
            m_KnotPos = EditorGUILayout.Vector3Field("Knot", m_KnotPos);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_LeftTangentPos = EditorGUILayout.Vector3Field("Left Tangent", m_LeftTangentPos);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_RightTangentPos = EditorGUILayout.Vector3Field("Right Tangent", m_RightTangentPos);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Radius = EditorGUILayout.FloatField("Radius", m_Radius);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_Tilt = EditorGUILayout.FloatField("Tilt", m_Tilt);

            GUILayout.EndHorizontal();

            #endregion
        }
    }
}
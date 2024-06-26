using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XiheRendering.Procedural.VineMaker.Scripts;

namespace XiheRendering.Procedural.VineMaker.Editor {
    [CustomEditor(typeof(VineMaker))]
    public class VineMakerEditor : UnityEditor.Editor {
        SerializedProperty m_PrefabsProp;
        SerializedProperty m_CountProp;
        SerializedProperty m_UseOffsetProp;
        SerializedProperty m_OffsetProp;
        SerializedProperty m_ResolutionProp;

        private VineMaker m_Target;

        private bool m_ShowDebug = false;
        private bool m_EditMode = false;

        void OnEnable() {
            // Setup the SerializedProperties.
            m_PrefabsProp = serializedObject.FindProperty("prefabs");
            m_CountProp = serializedObject.FindProperty("count");
            m_UseOffsetProp = serializedObject.FindProperty("useOffset");
            m_OffsetProp = serializedObject.FindProperty("offset");
            m_ResolutionProp = serializedObject.FindProperty("resolution");

            m_Target = (global::XiheRendering.Procedural.VineMaker.VineMaker)target;
        }

        private void OnSceneGUI() {
            if (!m_EditMode) return;

            for (int i = 0; i < m_Target.controlPoints.Count; i++) {
                var controlPoint = m_Target.controlPoints[i];
                var knot = controlPoint.knot;

                EditorGUI.BeginChangeCheck();

                var newGlobalKnot = Handles.PositionHandle(m_Target.transform.TransformPoint(knot), Quaternion.identity);
                Handles.Label(newGlobalKnot, $"Knot {i}");

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(m_Target, "Move Control Point Knot");
                    EditorUtility.SetDirty(m_Target);

                    controlPoint.knot = m_Target.transform.InverseTransformPoint(newGlobalKnot);
                    var delta = m_Target.transform.InverseTransformDirection(newGlobalKnot - knot);
                    // controlPoint.leftTangent += delta;
                    // controlPoint.rightTangent += delta;

                    m_Target.controlPoints[i] = controlPoint;

                    m_Target.ComputePath();
                }

                EditorGUI.BeginChangeCheck();

                var leftTangent = controlPoint.leftTangent;
                var rightTangent = controlPoint.rightTangent;


                Handles.color = Color.yellow;
                Vector3 newGlobalLeftTangent;
                if (i == 0) {
                    newGlobalLeftTangent = newGlobalKnot;
                }
                else {
                    newGlobalLeftTangent = Handles.PositionHandle(m_Target.transform.TransformPoint(leftTangent), Quaternion.identity);
                    Handles.DrawLine(newGlobalKnot, newGlobalLeftTangent);
                }

                Vector3 newGlobalRightTangent;
                if (i == m_Target.controlPoints.Count - 1) {
                    newGlobalRightTangent = newGlobalKnot;
                }
                else {
                    newGlobalRightTangent = Handles.PositionHandle(m_Target.transform.TransformPoint(rightTangent), Quaternion.identity);
                    Handles.DrawLine(newGlobalKnot, newGlobalRightTangent);
                }

                Handles.color = default;

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(m_Target, "Move Control Point Tangent");
                    EditorUtility.SetDirty(m_Target);

                    controlPoint.leftTangent = m_Target.transform.InverseTransformPoint(newGlobalLeftTangent);
                    controlPoint.rightTangent = m_Target.transform.InverseTransformPoint(newGlobalRightTangent);

                    m_Target.controlPoints[i] = controlPoint;

                    m_Target.ComputePath();
                }
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_PrefabsProp, true);

            if (m_PrefabsProp.isArray && m_PrefabsProp.arraySize > 0) {
                EditorGUILayout.PropertyField(m_CountProp);

                EditorGUILayout.PropertyField(m_UseOffsetProp);

                if (m_UseOffsetProp.boolValue) {
                    EditorGUILayout.PropertyField(m_OffsetProp);
                }

                if (GUILayout.Button("Create Mesh")) {
                    m_Target.CreateMesh();
                }

                EditorGUILayout.Space();
            }
            else {
                return;
            }

            GUILayout.Label("Curve Settings", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            var newResolution = EditorGUILayout.IntSlider(m_ResolutionProp.intValue, 1, 100);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(m_Target, "Set Resolution");
                EditorUtility.SetDirty(m_Target);

                m_Target.resolution = newResolution;

                m_Target.ComputePath();
            }


            if (m_Target.cubicBezierPath == null) {
                m_Target.controlPoints = new List<ControlPoint>();
                m_Target.controlPoints.Add(new ControlPoint(Vector3.zero));
                m_Target.controlPoints.Add(new ControlPoint(Vector3.right * 3));
                m_Target.controlPoints.Add(new ControlPoint(Vector3.right * 6 + Vector3.up * 3));
                m_Target.ComputePath();
            }

            if (GUILayout.Button("Update Path")) {
                m_Target.ComputePath();
            }

            if (m_EditMode) {
                if (GUILayout.Button("Disable Edit Mode")) {
                    m_EditMode = false;
                }

                EditorGUILayout.LabelField("Control Points:", EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                for (var i = 0; i < m_Target.controlPoints.Count; i++) {
                    var controlPoint = m_Target.controlPoints[i];
                    controlPoint.knot = EditorGUILayout.Vector3Field($"Knot {i}", controlPoint.knot);
                }

                if (EditorGUI.EndChangeCheck()) {
                    Undo.RecordObject(m_Target, "Manually Set Control Points");
                    EditorUtility.SetDirty(m_Target);
                    m_Target.ComputePath();
                }

                if (GUILayout.Button("Add Control Point")) {
                    if (m_Target.controlPoints.Count == 0) {
                        m_Target.controlPoints.Add(new ControlPoint(Vector3.zero));
                    }
                    else {
                        m_Target.controlPoints.Add(new ControlPoint(m_Target.controlPoints[m_Target.controlPoints.Count - 1].knot + Vector3.right * 10));
                    }
                }

                if (GUILayout.Button("Remove Control Point")) {
                    if (m_Target.controlPoints.Count > 0) {
                        m_Target.controlPoints.RemoveAt(m_Target.controlPoints.Count - 1);
                    }
                }
            }

            if (!m_EditMode) {
                if (GUILayout.Button("Enable Edit Mode")) {
                    m_EditMode = true;
                }
            }

            if (GUILayout.Button("Snap Mesh To Curve")) {
                if (m_Target.cubicBezierPath == null || m_Target.cubicBezierPath.Path == null) {
                    m_Target.ComputePath();
                }

                m_Target.SnapMeshToPath();
            }

            if (m_Target.cubicBezierPath != null && m_Target.cubicBezierPath.Path != null) {
                m_ShowDebug = EditorGUILayout.Foldout(m_ShowDebug, "Debug Info");
                EditorGUI.BeginDisabledGroup(true);
                if (m_ShowDebug) {
                    for (var i = 0; i < m_Target.cubicBezierPath.Path.Length; i++) {
                        var matrix4X4 = m_Target.cubicBezierPath.Path[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.Vector3Field($"Position {i}", matrix4X4.GetPosition());
                        EditorGUILayout.EndHorizontal();
                    }
                }

                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
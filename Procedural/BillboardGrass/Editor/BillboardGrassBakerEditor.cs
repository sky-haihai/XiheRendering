using System;
using UnityEditor;
using UnityEngine;

namespace XiheRendering.Procedural.BillboardGrass.Editor {
    [UnityEditor.CustomEditor(typeof(BillboardGrassBaker))]
    public class BillboardGrassBakerEditor : UnityEditor.Editor {
        private BillboardGrassBaker m_Target;

        private void OnEnable() {
            m_Target = (BillboardGrassBaker)target;
        }

        public override void OnInspectorGUI() {
            //display instance count
            if (m_Target.grassRenderer == null) {
                m_Target.grassRenderer = m_Target.GetComponent<BillboardGrassRenderer>();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Density Map Baker", EditorStyles.boldLabel);
            m_Target.bakerHitLayerMask = EditorGUILayout.MaskField("Baker Hit LayerMask", m_Target.bakerHitLayerMask, UnityEditorInternal.InternalEditorUtility.layers);

            //Bake button
            EditorGUI.BeginDisabledGroup(m_Target.isBaking);
            if (GUILayout.Button("Bake Density & Height Map")) {
                m_Target.BakeDensityMap(m_Target.transform.position, m_Target.grassRenderer.dimension, m_Target.grassRenderer.density, OnBakeProgress, OnFinishBake);
            }

            EditorGUI.EndDisabledGroup();

            //Cancel button
            EditorGUI.BeginDisabledGroup(!m_Target.isBaking);
            if (GUILayout.Button("Cancel Bake")) {
                m_Target.StopBakeDensityMap();
            }

            EditorGUI.EndDisabledGroup();
        }

        void OnBakeProgress(int finished, int total) {
            if (m_Target.isBaking) {
                if (finished % 200 == 0) {
                    m_Target.UpdateRemainingTime(total - finished, 200);
                }

                EditorUtility.DisplayProgressBar("Density Map Baker", $"Baking Pixels...{finished}/{total} Remaining Time: {m_Target.remainingTimeStr}", (float)finished / total);
            }
            else {
                EditorUtility.ClearProgressBar();
            }
        }

        void OnFinishBake(string lastPath) {
            EditorUtility.ClearProgressBar();
            m_Target.isBaking = false;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace XiheRendering.Procedural.VertexDensityBaker.Editor {
    public class VertexDensityBakerEditorWindow : EditorWindow {
        private enum Channel {
            R,
            G,
            B,
            A
        }

        private enum SampleMode {
            Average,
            Max,
            Min,
        }

        private Mesh m_SourceMesh;
        private Channel m_TargetChannel;
        private SampleMode m_SampleMode;

        [MenuItem("XiheRendering/Vertex Density Baker")]
        private static void ShowWindow() {
            GetWindow<VertexDensityBakerEditorWindow>("Vertex Density Baker");
        }

        private void OnGUI() {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Source Mesh");
            m_SourceMesh = (Mesh)EditorGUILayout.ObjectField(m_SourceMesh, typeof(Mesh), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Channel");
            m_TargetChannel = (Channel)EditorGUILayout.EnumPopup(m_TargetChannel);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Sample Mode");
            m_SampleMode = (SampleMode)EditorGUILayout.EnumPopup(m_SampleMode);
            GUILayout.EndHorizontal();

            if (m_SourceMesh == null) {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Start Bake", GUILayout.Height(60))) {
                EditorCoroutineUtility.StartCoroutineOwnerless(StartBake());
            }

            GUI.enabled = true;
        }

        IEnumerator StartBake() {
            BakeVertexDensity();
            yield return null;
        }

        private void BakeVertexDensity() {
            EditorCoroutineUtility.StartCoroutineOwnerless(ComputeVertexNeighbors(vertexNeighbors => {
                EditorCoroutineUtility.StartCoroutineOwnerless(ComputeVertexDensity(vertexNeighbors, list => {
                    EditorUtility.ClearProgressBar();
                    ApplyDensityToVertexColor(list);
                }));
            }));
        }

        IEnumerator ComputeVertexNeighbors(Action<List<int>[]> onFinish) {
            var result = new List<int>[m_SourceMesh.vertexCount];

            for (int i = 0; i < result.Length; i++) {
                result[i] = new List<int>();
                if (i % 1000 == 0) {
                    yield return null;
                }
            }

            for (int i = 0; i < m_SourceMesh.triangles.Length; i += 3) {
                var vert1 = m_SourceMesh.triangles[i];
                var vert2 = m_SourceMesh.triangles[i + 1];
                var vert3 = m_SourceMesh.triangles[i + 2];

                result[vert1].Add(vert2);
                result[vert1].Add(vert3);

                result[vert2].Add(vert1);
                result[vert2].Add(vert3);

                result[vert3].Add(vert1);
                result[vert3].Add(vert2);
                if (i % 100 == 0) {
                    EditorUtility.DisplayProgressBar("Baking Vertex Density", "Baking...(1/2)", (float)i / m_SourceMesh.triangles.Length);
                    yield return null;
                }
            }

            for (int i = 0; i < result.Length; i++) {
                result[i] = result[i].Distinct().ToList();
                if (i % 1000 == 0) {
                    yield return null;
                }
            }

            onFinish(result);
        }

        IEnumerator ComputeVertexDensity(List<int>[] vertexNeighbors, Action<List<float>> onFinish) {
            var result = new List<float>();

            var upperBound = 0f;
            var lowerBound = float.MaxValue;

            for (int i = 0; i < m_SourceMesh.vertexCount; i++) {
                var neighbors = vertexNeighbors[i];
                var density = 0f;
                var maxDensity = 0f;
                var minDensity = float.MaxValue;

                foreach (var neighbor in neighbors) {
                    density += Vector3.Distance(m_SourceMesh.vertices[i], m_SourceMesh.vertices[neighbor]);
                    if (density > maxDensity) {
                        maxDensity = density;
                    }

                    if (density < minDensity) {
                        minDensity = density;
                    }
                }

                switch (m_SampleMode) {
                    case SampleMode.Average:
                        density /= neighbors.Count;
                        break;
                    case SampleMode.Max:
                        density = maxDensity;
                        break;
                    case SampleMode.Min:
                        density = minDensity;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (density > upperBound) {
                    upperBound = density;
                }

                if (density < lowerBound) {
                    lowerBound = density;
                }

                result.Add(density);

                if (i % 100 == 0) {
                    EditorUtility.DisplayProgressBar("Baking Vertex Density", "Baking...(2/2)", (float)i / m_SourceMesh.vertexCount);
                    yield return null;
                }
            }

            // Normalize
            Debug.Log($"Upper bound: {upperBound}, Lower bound: {lowerBound}");
            var range = upperBound - lowerBound;
            for (int i = 0; i < result.Count; i++) {
                result[i] = (result[i] - lowerBound) / range;
                if (i % 1000 == 0) {
                    yield return null;
                }
            }

            onFinish(result);
        }

        private void ApplyDensityToVertexColor(List<float> vertexDensity) {
            var colors = m_SourceMesh.colors.ToList();
            switch (m_TargetChannel) {
                case Channel.R:
                    for (int i = 0; i < vertexDensity.Count; i++) {
                        colors[i] = new Color(vertexDensity[i], colors[i].g, colors[i].b, colors[i].a);
                    }

                    break;
                case Channel.G:
                    for (int i = 0; i < vertexDensity.Count; i++) {
                        colors[i] = new Color(colors[i].r, vertexDensity[i], colors[i].b, colors[i].a);
                    }

                    break;
                case Channel.B:
                    for (int i = 0; i < vertexDensity.Count; i++) {
                        colors[i] = new Color(colors[i].r, colors[i].g, vertexDensity[i], colors[i].a);
                    }

                    break;
                case Channel.A:
                    for (int i = 0; i < vertexDensity.Count; i++) {
                        colors[i] = new Color(colors[i].r, colors[i].g, colors[i].b, vertexDensity[i]);
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            m_SourceMesh.SetColors(colors);

            EditorUtility.SetDirty(m_SourceMesh);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Bake Complete");
        }
    }
}
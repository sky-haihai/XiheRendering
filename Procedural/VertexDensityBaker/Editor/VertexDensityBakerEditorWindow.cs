using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OilPaint.Scripts.Editor {
    public class VertexDensityBakerEditorWindow : EditorWindow {
        private enum Channel {
            R,
            G,
            B,
            A
        }

        private Mesh m_SourceMesh;
        private Channel m_TargetChannel;

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

            if (m_SourceMesh == null) {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Swizzle", GUILayout.Height(60))) {
                BakeVertexDensity();
            }

            GUI.enabled = true;
        }

        private void BakeVertexDensity() {
            var vertexNeighbors = ComputeVertexNeighbors();
            var vertexDensity = ComputeVertexDensity(vertexNeighbors);

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
        }

        List<int>[] ComputeVertexNeighbors() {
            var result = new List<int>[m_SourceMesh.vertexCount];

            for (int i = 0; i < result.Length; i++) {
                result[i] = new List<int>();
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
            }

            for (int i = 0; i < result.Length; i++) {
                result[i] = result[i].Distinct().ToList();
            }

            return result;
        }

        List<float> ComputeVertexDensity(List<int>[] vertexNeighbors) {
            var result = new List<float>();

            var upperBound = 0f;
            var lowerBound = float.MaxValue;

            for (int i = 0; i < m_SourceMesh.vertexCount; i++) {
                var neighbors = vertexNeighbors[i];
                var density = 0f;

                foreach (var neighbor in neighbors) {
                    density += Vector3.Distance(m_SourceMesh.vertices[i], m_SourceMesh.vertices[neighbor]);
                    if (density > upperBound) {
                        density = upperBound;
                    }

                    if (density < lowerBound) {
                        density = lowerBound;
                    }
                }

                result.Add(density);
            }

            // Normalize
            var range = upperBound - lowerBound;
            for (int i = 0; i < result.Count; i++) {
                result[i] = (result[i] - lowerBound) / range;
            }

            return result;
        }
    }
}
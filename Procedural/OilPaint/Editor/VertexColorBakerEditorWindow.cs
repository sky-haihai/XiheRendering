using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using XiheRendering.Utility.VertexDensityBaker.Editor;

namespace XiheRendering.Procedural.OilPaint.Editor {
    public class VertexColorBakerEditorWindow : EditorWindow {
        public enum SampleMode {
            Average,
            Max,
            Min,
        }

        private enum UvChannel {
            UV0 = 0,
            UV1 = 1,
            UV2 = 2,
            UV3 = 3,
        }

        //density
        private Mesh m_SourceMesh;
        private SampleMode m_SampleMode;
        private int m_VertexCount;
        private bool m_Baking;
        // private EditorCoroutine m_Handle;
        private int m_TotalProcessCount;
        private int m_TriangleCount;

        //texture to vertex color
        private Texture2D m_Texture;
        private UvChannel m_UvChannel;

        //cache
        private Color[] m_SrcColors;
        private int[] m_SrcTriangles;

        private List<int>[] m_VertexNeighbors;
        private List<float> m_VertexDensity;
        private Color[] m_ColorResult;

        private DateTime m_StartTime;

        [MenuItem("Tools/PaintRenderer/Vertex Color Baker")]
        private static void ShowWindow() {
            GetWindow<VertexColorBakerEditorWindow>("Vertex Color Baker");
        }

        private void OnGUI() {
            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Source Mesh");
            m_SourceMesh = (Mesh)EditorGUILayout.ObjectField(m_SourceMesh, typeof(Mesh), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Texture");
            m_Texture = (Texture2D)EditorGUILayout.ObjectField(m_Texture, typeof(Texture2D), false);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("UV Channel");
            m_UvChannel = (UvChannel)EditorGUILayout.EnumPopup(m_UvChannel);
            GUILayout.EndHorizontal();

            //display uv count
            GUILayout.BeginHorizontal();
            var uvCount = 0;
            if (m_SourceMesh != null) {
                switch (m_UvChannel) {
                    case UvChannel.UV0:
                        uvCount = m_SourceMesh.uv.Length;
                        break;
                    case UvChannel.UV1:
                        uvCount = m_SourceMesh.uv2.Length;
                        break;
                    case UvChannel.UV2:
                        uvCount = m_SourceMesh.uv3.Length;
                        break;
                    case UvChannel.UV3:
                        uvCount = m_SourceMesh.uv4.Length;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (uvCount == 0 && m_SourceMesh != null) {
                EditorGUILayout.HelpBox($"UV {(int)m_UvChannel} is empty", MessageType.Warning);
            }

            GUILayout.EndHorizontal();

            if (m_SourceMesh == null || m_Texture == null || uvCount == 0 || !m_SourceMesh.isReadable) {
                GUI.enabled = false;
            }

            GUI.enabled = true;

            GUILayout.BeginHorizontal();
            GUILayout.Label("Density Sample Mode");
            m_SampleMode = (SampleMode)EditorGUILayout.EnumPopup(m_SampleMode);
            GUILayout.EndHorizontal();

            if (m_Baking || m_SourceMesh == null || m_Texture == null || uvCount == 0 || !m_SourceMesh.isReadable || !m_Texture.isReadable) {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Bake", GUILayout.Height(40))) {
                m_Baking = true;
                // m_Handle = EditorCoroutineUtility.StartCoroutineOwnerless(StartBake());
            }

            GUI.enabled = true;

            if (m_Baking) {
                EditorGUILayout.HelpBox("Baking...", MessageType.Info);
            }

            if (m_SourceMesh != null) {
                GUI.skin.label.fontSize = 14;
                GUILayout.Label("Debug Info");
                GUI.skin.label.fontSize = 12;

                string vertexInfo = "Not Readable";
                string colorInfo = "Not Readable";
                string triangleInfo = "Not Readable";

                if (!m_SourceMesh.isReadable) {
                    EditorGUILayout.HelpBox("Mesh is not readable", MessageType.Error);
                    m_SrcColors = Array.Empty<Color>();
                    m_SrcTriangles = Array.Empty<int>();

                    vertexInfo = "Not Readable";
                    colorInfo = "Not Readable";
                    triangleInfo = "Not Readable";
                }
                else {
                    m_SrcColors = m_SourceMesh.colors;
                    m_SrcTriangles = m_SourceMesh.triangles;

                    vertexInfo = m_SourceMesh.vertexCount.ToString();
                    colorInfo = m_SrcColors.Length.ToString();
                    triangleInfo = (m_SrcTriangles.Length / 3).ToString();
                }

                if (m_Texture != null && !m_Texture.isReadable) {
                    EditorGUILayout.HelpBox("Texture is not readable", MessageType.Error);
                }


                GUILayout.BeginHorizontal();
                GUILayout.Label("Vertex Count");
                GUILayout.Label(vertexInfo);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Color Count");
                GUILayout.Label(colorInfo);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Triangle Count");
                GUILayout.Label(triangleInfo);
                GUILayout.EndHorizontal();
            }
        }

        private IEnumerator StartBake() {
            if (EditorUtility.DisplayCancelableProgressBar("Hold On", "Baking Vertex Color(RGB)...", 0)) {
                StopBake();
            }

            m_SrcColors = m_SourceMesh.colors;
            m_SrcTriangles = m_SourceMesh.triangles;
            m_ColorResult = new Color[m_SourceMesh.vertexCount];
            m_StartTime = DateTime.Now;

            yield return BakeTextureToVertexColor();
            yield return BakeVertexDensity();
            ApplyDensityToVertexColor();

            EditorUtility.SetDirty(m_SourceMesh);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();
            
            var duration = DateTime.Now - m_StartTime;
            Debug.Log($"Hi! OcOilPaint Vertex Color Bake success. Duration: {duration.TotalSeconds:0.0}s");

            m_Baking = false;
            m_VertexNeighbors = null;
            m_VertexDensity = null;
            m_ColorResult = null;
        }

        void StopBake() {
            if (m_Baking) {
                // EditorCoroutineUtility.StopCoroutine(m_Handle);
                m_Baking = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private IEnumerator BakeTextureToVertexColor() {
            Vector2[] uvs;
            switch (m_UvChannel) {
                case UvChannel.UV0:
                    uvs = m_SourceMesh.uv;
                    break;
                case UvChannel.UV1:
                    uvs = m_SourceMesh.uv2;
                    break;
                case UvChannel.UV2:
                    uvs = m_SourceMesh.uv3;
                    break;
                case UvChannel.UV3:
                    uvs = m_SourceMesh.uv4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            for (var i = 0; i < m_SourceMesh.vertexCount; i++) {
                var uv = uvs[i];
                var color = m_Texture.GetPixelBilinear(uv.x, uv.y);
                color.r = Mathf.Pow(color.r, 2.2f);
                color.g = Mathf.Pow(color.g, 2.2f);
                color.b = Mathf.Pow(color.b, 2.2f);

                m_ColorResult[i] = color;

                //progress
                if (i % 1000 == 0) {
                    if (EditorUtility.DisplayCancelableProgressBar("Hold On", "Baking vertex color(RGB)...", (float)i / m_SourceMesh.vertexCount)) {
                        StopBake();
                    }

                    yield return null;
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private IEnumerator BakeVertexDensity() {
            m_VertexCount = m_SourceMesh.vertexCount;
            m_TriangleCount = m_SrcTriangles.Length;
            m_TotalProcessCount = m_VertexCount * 2 + m_TriangleCount;

            yield return ComputeVertexNeighbors();
            yield return ComputeVertexDensity();
        }

        IEnumerator ComputeVertexNeighbors() {
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
                if (i % 1000 == 0) {
                    var progress = (float)i / m_TotalProcessCount;
                    if (EditorUtility.DisplayCancelableProgressBar("Hold On", "Baking Vertex Density(A)...", progress)) {
                        StopBake();
                    }

                    yield return null;
                }
            }

            for (int i = 0; i < result.Length; i++) {
                result[i] = result[i].Distinct().ToList();
            }

            m_VertexNeighbors = result;
        }

        IEnumerator ComputeVertexDensity() {
            var result = new List<float>();

            var upperBound = 0f;
            var lowerBound = float.MaxValue;

            for (int i = 0; i < m_SourceMesh.vertexCount; i++) {
                var neighbors = m_VertexNeighbors[i];
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

                if (i % 1000 == 0) {
                    var progress = (float)(i + m_TriangleCount) / m_TotalProcessCount;
                    if (EditorUtility.DisplayCancelableProgressBar("Hold On", "Baking vertex density(A)...", progress)) {
                        StopBake();
                    }

                    yield return null;
                }
            }

            // Normalize
            var range = upperBound - lowerBound;
            for (int i = 0; i < result.Count; i++) {
                result[i] = (result[i] - lowerBound) / range;
                if (i % 1000 == 0) {
                    var progress = (float)(i + m_TriangleCount + m_VertexCount) / m_TotalProcessCount;
                    if (EditorUtility.DisplayCancelableProgressBar("Hold On", "Baking Vertex Density(A)...", progress)) {
                        StopBake();
                    }

                    yield return null;
                }
            }

            m_VertexDensity = result;
        }

        private void ApplyDensityToVertexColor() {
            for (int i = 0; i < m_VertexDensity.Count; i++) {
                m_ColorResult[i].a = m_VertexDensity[i];
            }

            m_SourceMesh.SetColors(m_ColorResult);
        }
    }
}
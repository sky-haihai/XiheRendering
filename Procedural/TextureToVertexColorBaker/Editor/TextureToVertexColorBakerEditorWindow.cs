﻿using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace XiheRendering.Procedural.TextureToVertexColorBaker.Editor {
    public class TextureToVertexColorBakerEditorWindow : EditorWindow {
        private enum UvChannel {
            UV0 = 0,
            UV1 = 1,
            UV2 = 2,
            UV3 = 3,
        }

        private Mesh m_SourceMesh;
        private Texture2D m_Texture;
        private UvChannel m_UvChannel;


        [MenuItem("XiheRendering/Texture To Vertex Color Baker")]
        static void ShowWindow() {
            GetWindow<TextureToVertexColorBakerEditorWindow>("Texture To Vertex Color Baker");
        }

        private void OnGUI() {
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

            if (m_SourceMesh == null || m_Texture == null) {
                GUI.enabled = false;
            }

            if (GUILayout.Button("Start Bake", GUILayout.Height(60))) {
                EditorCoroutineUtility.StartCoroutineOwnerless(StartBake());
            }

            GUI.enabled = true;
        }

        private IEnumerator StartBake() {
            var vertices = m_SourceMesh.vertices;
            var uvs = m_SourceMesh.uv;
            var colors = new Color[vertices.Length];

            for (var i = 0; i < vertices.Length; i++) {
                var uv = uvs[i];
                switch (m_UvChannel) {
                    case UvChannel.UV0:
                        var color = m_Texture.GetPixelBilinear(uv.x, uv.y);
                        color.r = Mathf.Pow(color.r, 2.2f);
                        color.g = Mathf.Pow(color.g, 2.2f);
                        color.b = Mathf.Pow(color.b, 2.2f);
                        colors[i] = color;
                        break;
                    case UvChannel.UV1:
                        colors[i] = m_Texture.GetPixelBilinear(uv.x, uv.y);
                        break;
                    case UvChannel.UV2:
                        colors[i] = m_Texture.GetPixelBilinear(uv.x, uv.y);
                        break;
                    case UvChannel.UV3:
                        colors[i] = m_Texture.GetPixelBilinear(uv.x, uv.y);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                //progress
                if (i % 100 == 0) {
                    EditorUtility.DisplayProgressBar("Baking", "Baking vertex color...", (float)i / vertices.Length);
                    yield return null;
                }
            }

            m_SourceMesh.colors = colors;

            EditorUtility.ClearProgressBar();

            yield return null;

            //clear
            EditorUtility.ClearProgressBar();

            //set dirty and save
            EditorUtility.SetDirty(m_SourceMesh);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Bake Complete");
        }
    }
}
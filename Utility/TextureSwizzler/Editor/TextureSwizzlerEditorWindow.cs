using System;
using UnityEditor;
using UnityEngine;

namespace XiheRendering.Utility.TextureSwizzler.Editor {
    public class TextureSwizzlerEditorWindow : EditorWindow {
        private Texture2D m_RChannelTex;
        private Texture2D m_GChannelTex;
        private Texture2D m_BChannelTex;
        private Texture2D m_AChannelTex;

        private enum Channel {
            R,
            G,
            B,
            A
        }

        private Channel m_RTexSelectedChannel = Channel.R;
        private Channel m_GTexSelectedChannel = Channel.R;
        private Channel m_BTexSelectedChannel = Channel.R;
        private Channel m_ATexSelectedChannel = Channel.R;
        private bool m_GammaCorrectionAlpha = false;

        private RenderTexture m_ResultRt;
        private Texture2D m_ResultTex2D;
        private Material m_BlitMaterial;

        private int m_OutputWidth = 128;
        private int m_OutputHeight = 128;

        private string m_LastSavePath;

        private static readonly int RChannelTexPropID = Shader.PropertyToID("_RChannelTex");
        private static readonly int GChannelTexPropID = Shader.PropertyToID("_GChannelTex");
        private static readonly int BChannelTexPropID = Shader.PropertyToID("_BChannelTex");
        private static readonly int AChannelTexPropID = Shader.PropertyToID("_AChannelTex");
        private static readonly int RTexChannelMaskPropID = Shader.PropertyToID("_RTexChannelMask");
        private static readonly int GTexChannelMaskPropID = Shader.PropertyToID("_GTexChannelMask");
        private static readonly int BTexChannelMaskPropID = Shader.PropertyToID("_BTexChannelMask");
        private static readonly int ATexChannelMaskPropID = Shader.PropertyToID("_ATexChannelMask");
        private static readonly int GammaCorrectionAlphaPropID = Shader.PropertyToID("_GammaCorrectionAlpha");

        [MenuItem("XiheRendering/Texture Swizzler Window")]
        public static void ShowWindow() {
            GetWindow<TextureSwizzlerEditorWindow>("Texture Swizzler");
        }

        private void OnGUI() {
            CreatePlaceholderTextures();

            #region Data

            GUILayout.BeginHorizontal();
            GUILayout.Label("R Channel Texture", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_RChannelTex = (Texture2D)EditorGUILayout.ObjectField(m_RChannelTex, typeof(Texture2D), false, GUILayout.Height(64), GUILayout.Width(64));
            GUILayout.BeginVertical();
            GUILayout.Label("Channel:");
            m_RTexSelectedChannel = (Channel)EditorGUILayout.EnumPopup(m_RTexSelectedChannel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("G Channel Texture", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_GChannelTex = (Texture2D)EditorGUILayout.ObjectField(m_GChannelTex, typeof(Texture2D), false, GUILayout.Height(64), GUILayout.Width(64));
            GUILayout.BeginVertical();
            GUILayout.Label("Channel:");
            m_GTexSelectedChannel = (Channel)EditorGUILayout.EnumPopup(m_GTexSelectedChannel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("B Channel Texture", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_BChannelTex = (Texture2D)EditorGUILayout.ObjectField(m_BChannelTex, typeof(Texture2D), false, GUILayout.Height(64), GUILayout.Width(64));
            GUILayout.BeginVertical();
            GUILayout.Label("Channel:");
            m_BTexSelectedChannel = (Channel)EditorGUILayout.EnumPopup(m_BTexSelectedChannel);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("A Channel Texture", EditorStyles.boldLabel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            m_AChannelTex = (Texture2D)EditorGUILayout.ObjectField(m_AChannelTex, typeof(Texture2D), false, GUILayout.Height(64), GUILayout.Width(64));
            GUILayout.BeginVertical();
            GUILayout.Label("Channel:");
            m_ATexSelectedChannel = (Channel)EditorGUILayout.EnumPopup(m_ATexSelectedChannel);
            m_GammaCorrectionAlpha = EditorGUILayout.Toggle("Gamma Correction", m_GammaCorrectionAlpha);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            #endregion

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            m_OutputWidth = EditorGUILayout.IntField("Output Width", m_OutputWidth);
            m_OutputWidth = Mathf.Max(1, m_OutputWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            m_OutputHeight = EditorGUILayout.IntField("Output Height", m_OutputHeight);
            m_OutputHeight = Mathf.Max(1, m_OutputHeight);
            GUILayout.EndHorizontal();

            //bake button
            if (GUILayout.Button("Swizzle", GUILayout.Height(60))) {
                SwizzleTextures();
                SaveTextureToFile();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset")) {
                m_RChannelTex = null;
                m_GChannelTex = null;
                m_BChannelTex = null;
                m_AChannelTex = null;
                m_RTexSelectedChannel = Channel.R;
                m_GTexSelectedChannel = Channel.R;
                m_BTexSelectedChannel = Channel.R;
                m_ATexSelectedChannel = Channel.R;
                if (m_ResultRt != null) {
                    m_ResultRt.Release();
                    m_ResultRt = null;
                }

                m_ResultTex2D = null;
            }

            GUILayout.EndHorizontal();

            #region Debug

            // EditorGUILayout.Space();
            // GUILayout.BeginHorizontal();
            // GUILayout.Label("Debug", EditorStyles.boldLabel);
            // GUILayout.EndHorizontal();
            //
            // GUILayout.BeginHorizontal();
            // if (m_ResultRt) {
            //     GUILayout.BeginVertical();
            //     GUILayout.Label("RT", EditorStyles.label);
            //     EditorGUILayout.ObjectField(m_ResultRt, typeof(RenderTexture), false, GUILayout.Height(128), GUILayout.Width(128));
            //     GUILayout.EndVertical();
            // }
            //
            // if (m_ResultTex2D) {
            //     GUILayout.BeginVertical();
            //     GUILayout.Label("Tex2D", EditorStyles.label);
            //     EditorGUILayout.ObjectField(m_ResultTex2D, typeof(Texture2D), false, GUILayout.Height(128), GUILayout.Width(128));
            //     GUILayout.EndVertical();
            // }
            //
            // GUILayout.EndHorizontal();

            #endregion
        }

        //real magic happens here
        private void SwizzleTextures() {
            if (m_ResultRt != null) {
                m_ResultRt.Release();
                m_ResultRt = null;
            }

            if (m_BlitMaterial == null) {
                m_BlitMaterial = new Material(Shader.Find("Hidden/XiheRendering/SwizzleBlit"));
            }

            m_ResultRt = new RenderTexture(m_RChannelTex.width, m_RChannelTex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            m_ResultRt.enableRandomWrite = true;
            m_ResultRt.Create();

            m_BlitMaterial.SetTexture(RChannelTexPropID, m_RChannelTex);
            m_BlitMaterial.SetTexture(GChannelTexPropID, m_GChannelTex);
            m_BlitMaterial.SetTexture(BChannelTexPropID, m_BChannelTex);
            m_BlitMaterial.SetTexture(AChannelTexPropID, m_AChannelTex);
            SetChannelMask(m_RTexSelectedChannel, RTexChannelMaskPropID);
            SetChannelMask(m_GTexSelectedChannel, GTexChannelMaskPropID);
            SetChannelMask(m_BTexSelectedChannel, BTexChannelMaskPropID);
            SetChannelMask(m_ATexSelectedChannel, ATexChannelMaskPropID);
            m_BlitMaterial.SetFloat(GammaCorrectionAlphaPropID, m_GammaCorrectionAlpha ? 2.2f : 1f);

            Graphics.Blit(m_RChannelTex, m_ResultRt, m_BlitMaterial);

            m_ResultTex2D = new Texture2D(m_OutputWidth, m_OutputHeight, TextureFormat.ARGB32, false, true);
            RenderTexture.active = m_ResultRt;
            m_ResultTex2D.ReadPixels(new Rect(0, 0, m_OutputWidth, m_OutputHeight), 0, 0);
            RenderTexture.active = null;
            m_ResultTex2D.Apply();
        }

        private void SaveTextureToFile() {
            if (String.IsNullOrEmpty(m_LastSavePath)) {
                m_LastSavePath = Application.dataPath;
            }

            var path = EditorUtility.SaveFilePanel("Save Swizzled Texture", m_LastSavePath, "SwizzledTexture", "png");
            m_LastSavePath = path;
            if (path.Length != 0) {
                System.IO.File.WriteAllBytes(path, m_ResultTex2D.EncodeToPNG());
                AssetDatabase.Refresh();
            }
        }

        void SetChannelMask(Channel channel, int channelMaskPropId) {
            switch (channel) {
                case Channel.R:
                    m_BlitMaterial.SetVector(channelMaskPropId, new Vector4(1, 0, 0, 0));
                    break;
                case Channel.G:
                    m_BlitMaterial.SetVector(channelMaskPropId, new Vector4(0, 1, 0, 0));
                    break;
                case Channel.B:
                    m_BlitMaterial.SetVector(channelMaskPropId, new Vector4(0, 0, 1, 0));
                    break;
                case Channel.A:
                    m_BlitMaterial.SetVector(channelMaskPropId, new Vector4(0, 0, 0, 1));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(channel), channel, null);
            }
        }

        private void CreatePlaceholderTextures() {
            if (m_RChannelTex == null) {
                m_RChannelTex = Texture2D.blackTexture;
                m_RChannelTex.Reinitialize(m_OutputWidth, m_OutputHeight, TextureFormat.ARGB32, false);
            }

            if (m_GChannelTex == null) {
                m_GChannelTex = Texture2D.blackTexture;
                m_GChannelTex.Reinitialize(m_OutputWidth, m_OutputHeight, TextureFormat.ARGB32, false);
            }

            if (m_BChannelTex == null) {
                m_BChannelTex = Texture2D.blackTexture;
                m_BChannelTex.Reinitialize(m_OutputWidth, m_OutputHeight, TextureFormat.ARGB32, false);
            }

            if (m_AChannelTex == null) {
                m_AChannelTex = Texture2D.blackTexture;
                m_AChannelTex.Reinitialize(m_OutputWidth, m_OutputHeight, TextureFormat.ARGB32, false);
            }
        }
    }
}
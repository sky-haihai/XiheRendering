#if UNITY_EDITOR
using System;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace XiheRendering.Procedural.BillboardGrass {
    [ExecuteInEditMode, RequireComponent(typeof(BillboardGrassRenderer))]
    public class BillboardGrassBaker : MonoBehaviour {
        public BillboardGrassRenderer grassRenderer;
        public LayerMask bakerHitLayerMask = 0;
        public bool isBaking;
        public string remainingTimeStr;

        private EditorCoroutine m_BakeTexturesCoroutine;
        private string m_LastBakePath;
        private DateTime m_CachedTime = DateTime.MinValue;

        public void BakeDensityMap(Vector3 bakeVolumeCenter, Vector3 bakeVolumeDimension, Vector2 density, Action<int, int> onProgress = null,
            Action<string> onBakeFinished = null) {
            var pixelCountX = Mathf.FloorToInt(density.x * bakeVolumeDimension.x);
            var pixelCountY = Mathf.FloorToInt(density.y * bakeVolumeDimension.z);
            Texture2D densityTexture = new Texture2D(pixelCountX, pixelCountY, TextureFormat.R16, false, true);
            Texture2D heightTexture = new Texture2D(pixelCountX, pixelCountY, TextureFormat.R16, false, true);
            onBakeFinished += (path) => {
                m_LastBakePath = path;
                m_BakeTexturesCoroutine = null;
            };

            var bakeTexturesCo = DensityMapBakeHelper.BakeDensityAndHeightMapCo(densityTexture, heightTexture, bakeVolumeCenter, bakeVolumeDimension, pixelCountX, pixelCountY,
                bakerHitLayerMask, m_LastBakePath, onProgress, onBakeFinished);
            m_BakeTexturesCoroutine = EditorCoroutineUtility.StartCoroutine(bakeTexturesCo, this);

            isBaking = true;
        }

        public void UpdateRemainingTime(int remainingCount, int intervalCount) {
            if (m_CachedTime == DateTime.MinValue) {
                m_CachedTime = DateTime.Now;
            }

            var timeSpanMinutes = (DateTime.Now - m_CachedTime).TotalMinutes;
            if (timeSpanMinutes < 0.0001f) {
                m_CachedTime = DateTime.Now;
                remainingTimeStr = "Calculating...";
                return;
            }

            var speed = intervalCount / timeSpanMinutes;
            var remainingTime = TimeSpan.FromMinutes(remainingCount / speed);
            m_CachedTime = DateTime.Now;
            remainingTimeStr = remainingTime.ToString(@"hh\:mm\:ss");
        }

        private void Update() {
            if (m_BakeTexturesCoroutine == null) {
                isBaking = false;
            }
        }

        public void StopBakeDensityMap() {
            if (m_BakeTexturesCoroutine != null) {
                EditorCoroutineUtility.StopCoroutine(m_BakeTexturesCoroutine);
            }

            isBaking = false;
            EditorUtility.ClearProgressBar();
        }
    }
}
#endif
#if UNITY_EDITOR
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace XiheRendering.Procedural.BillboardGrass {
    public static class DensityMapBakeHelper {
        // ReSharper disable Unity.PerformanceAnalysis
        public static IEnumerator BakeDensityAndHeightMapCo(Texture2D densityTexture, Texture2D heightTexture, Vector3 bakeVolumeCenter, Vector3 bakeVolumeDimension,
            int pixelCountX, int pixelCountY, LayerMask bakerHitLayerMask, string lastBakePath, Action<int, int> onProgress = null, Action<string> onBakeFinished = null) {
            var startY = bakeVolumeCenter.y + bakeVolumeDimension.y / 2;
            var firstPos = new Vector3(bakeVolumeCenter.x - bakeVolumeDimension.x / 2, 0, bakeVolumeCenter.z - bakeVolumeDimension.z / 2);
            Debug.Log($"Baking started. Coffee time!");

            for (int i = 0; i < pixelCountX; i++) {
                for (int j = 0; j < pixelCountY; j++) {
                    var startPos = firstPos + new Vector3(i * bakeVolumeDimension.x / pixelCountX, startY, j * bakeVolumeDimension.z / pixelCountY);
                    GatherGrassInfo(startPos, bakeVolumeCenter.y, bakeVolumeDimension.y, bakerHitLayerMask, out var height, out var density);
                    // Debug.DrawLine(startPos, startPos + Vector3.down * bakeVolumeDimension.y, Color.HSVToRGB(0, density, height));
                    densityTexture.SetPixel(i, j, new Color(density, 0, 0, 0));
                    heightTexture.SetPixel(i, j, new Color(height, 0, 0, 0));

                    onProgress?.Invoke(i * pixelCountY + j, pixelCountX * pixelCountY);

                    SceneView.RepaintAll();
                    yield return null;
                }
            }

            densityTexture.Apply();


            if (String.IsNullOrEmpty(lastBakePath)) {
                lastBakePath = Application.dataPath;
            }

            //save to file
            var path = EditorUtility.SaveFilePanel("Save Density Map", lastBakePath, "DensityMap", "png");
            if (path.Length != 0) {
                System.IO.File.WriteAllBytes(path, densityTexture.EncodeToPNG());
            }

            lastBakePath = path;

            path = EditorUtility.SaveFilePanel("Save Height Map", lastBakePath, "HeightMap", "png");
            if (path.Length != 0) {
                System.IO.File.WriteAllBytes(path, heightTexture.EncodeToPNG());
            }

            lastBakePath = path;

            AssetDatabase.Refresh();
            onBakeFinished?.Invoke(lastBakePath);
        }

        private static void GatherGrassInfo(Vector3 rayStartPoint, float volumePosY, float maxDistance, LayerMask layerMask, out float height, out float density) {
            Ray ray = new Ray(rayStartPoint, Vector3.down);
            LayerMask everythingMask = ~0;
            Physics.queriesHitBackfaces = true;
            var hit = Physics.Raycast(ray, out var hitInfo, maxDistance, everythingMask, QueryTriggerInteraction.Ignore);
            if (!hit) {
                height = 0;
                density = 0;
                return;
            }

            var include = layerMask == (layerMask | (1 << hitInfo.collider.gameObject.layer));
            if (!include) {
                height = 0;
                density = 0;
                return;
            }

            var dot = Vector3.Dot(Vector3.up, hitInfo.normal);
            dot = dot * 0.5f + 0.5f;
            density = dot;

            var hitY = hitInfo.point.y;
            var t = (hitY - (volumePosY - maxDistance / 2)) / maxDistance;
            height = Mathf.Lerp(0, 1, t);
        }
    }
}
#endif
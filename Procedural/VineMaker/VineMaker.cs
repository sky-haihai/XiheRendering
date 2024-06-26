using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XiheRendering.Procedural.VineMaker.Scripts;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

namespace XiheRendering.Procedural.VineMaker {
    public class VineMaker : MonoBehaviour {
#if UNITY_EDITOR
        //array
        public Renderer[] prefabs;
        public int count = 1;
        public float offset;
        public bool useOffset;
        public List<MeshFilter> meshFilters = new List<MeshFilter>();

        //curve path
        public List<ControlPoint> controlPoints = new List<ControlPoint>();
        public CubicBezierPath cubicBezierPath;
        public int resolution = 10;

        //snap mesh to curve
        private List<Mesh> m_CachedMeshes = new List<Mesh>();

        public void CreateMesh() {
            //clear all children transforms
            foreach (Transform t in transform) {
                EditorApplication.delayCall += () => { Undo.DestroyObjectImmediate(t.gameObject); };
            }

            meshFilters.Clear();
            m_CachedMeshes.Clear();
            float lastPosX = 0;
            for (var i = 0; i < count; i++) {
                //pick a random prefab
                var pick = Random.Range(0, prefabs.Length);
                var go = PrefabUtility.InstantiatePrefab(prefabs[pick].gameObject, transform) as GameObject;
                if (go == null) continue;
                var r = go.GetComponent<Renderer>();

                //offset pos
                float posX = 0f;
                if (useOffset) {
                    posX = i * offset;
                }
                else {
                    var bounds = r.bounds;
                    Vector3 localBoundMin = go.transform.InverseTransformPoint(bounds.min);
                    posX = lastPosX - localBoundMin.x;
                    lastPosX += bounds.size.x;
                }

                go.transform.localPosition = posX * Vector3.right;

                //add mesh filter
                var meshFilter = go.GetComponent<MeshFilter>();
                meshFilters.Add(meshFilter);
                var copyMesh = Instantiate(meshFilter.sharedMesh);
                m_CachedMeshes.Add(copyMesh);
            }
        }

        public void ComputePath() {
            if (controlPoints == null) return;
            if (controlPoints.Count < 2) return;

            var temp = new List<ControlPoint>();
            foreach (var point in controlPoints) {
                var globalKnot = transform.TransformPoint(point.knot);
                var globalLeftTangent = transform.TransformPoint(point.leftTangent);
                var globalRightTangent = transform.TransformPoint(point.rightTangent);
                temp.Add(new ControlPoint(globalKnot, globalLeftTangent, globalRightTangent, point.radius, point.tilt));
            }

            cubicBezierPath = new CubicBezierPath(temp.ToArray());
            cubicBezierPath.CreateUniformPath(resolution, 0.1f);
        }

        public void SnapMeshToPath() {
            for (var i = 0; i < meshFilters.Count; i++) {
                var mesh = m_CachedMeshes[i];
                var meshFilter = meshFilters[i];
                var copyMesh = Instantiate(mesh);
                var verts = copyMesh.vertices;
                for (var j = 0; j < verts.Length; j++) {
                    var vert = verts[j];
                    var worldPos = meshFilter.transform.TransformPoint(vert);
                    var pathOriginToVert = worldPos - transform.position; //vector from path origin to current vert
                    var distanceOnPath = pathOriginToVert.x;

                    var localPos = new Vector3(-verts[j].z, verts[j].y, 0);
                    var matrix = cubicBezierPath.GetTRSMatrixAt(distanceOnPath);
                    var newPos = matrix.MultiplyPoint(localPos);
                    var newLocalPos = meshFilter.transform.InverseTransformPoint(newPos);

                    // var distance = worldPos.x - transform.position.x;
                    // var matrix = cubicBezierPath.GetTRSMatrixAt(distance);
                    // var localPos = worldPos - transform.position;
                    // localPos.x = 0;
                    // localPos = new Vector3(-localPos.z, localPos.y, 0);
                    // var newPos = matrix.MultiplyPoint(localPos);
                    // var newLocalPos = meshFilter.transform.InverseTransformPoint(newPos);
                    verts[j] = newLocalPos;
                }

                copyMesh.vertices = verts;
                copyMesh.RecalculateBounds();
                copyMesh.RecalculateNormals();
                meshFilters[i].mesh = copyMesh;
            }
        }

        private Vector3 TransformVertexToPath(Vector3 vertexOS, Transform meshFilterTransform) {
            var worldPos = meshFilterTransform.TransformPoint(vertexOS);
            var pathOriginToVert = worldPos - transform.position; //vector from path origin to current vert
            var distanceOnPath = pathOriginToVert.x;

            var offsetFromXAxis = new Vector3(-vertexOS.z, vertexOS.y, 0);
            var matrix = cubicBezierPath.GetTRSMatrixAt(distanceOnPath);
            return Vector3.zero;
        }


        private void OnDrawGizmos() {
            foreach (Transform t in transform) {
                //draw bound box
                var r = t.GetComponent<Renderer>();
                if (r == null) continue;
                var bounds = r.bounds;
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }

            //draw path
            if (cubicBezierPath == null) return;
            if (cubicBezierPath.Path == null) return;

            for (int i = 0; i < cubicBezierPath.Path.Length - 1; i++) {
                var matrix = cubicBezierPath.Path[i];
                Vector3 scale;
                scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
                scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
                scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
                Gizmos.color = Color.grey;
                Gizmos.DrawWireSphere(matrix.GetPosition(), 0.05f);
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(matrix.GetPosition(), cubicBezierPath.Path[i + 1].GetPosition());
                Gizmos.color = Color.red;
                Gizmos.DrawLine(matrix.GetPosition(), matrix.GetPosition() + new Vector3(matrix.m00, matrix.m10, matrix.m20) * scale.x);
                Gizmos.color = Color.green;
                Gizmos.DrawLine(matrix.GetPosition(), matrix.GetPosition() + new Vector3(matrix.m01, matrix.m11, matrix.m21) * scale.y);
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(matrix.GetPosition(), matrix.GetPosition() + new Vector3(matrix.m02, matrix.m12, matrix.m22) * scale.z);
            }

            // for (int i = 0; i < 100; i++) {
            //     var matrix = cubicBezierPath.GetTRSMatrixAt(i * 0.1f);
            //     Gizmos.matrix = matrix;
            //     Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            // }

            Gizmos.matrix = Matrix4x4.identity;
        }
#endif
    }
}
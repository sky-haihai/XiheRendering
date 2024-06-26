using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace XiheRendering.Procedural.VineMaker.Scripts {
    [Serializable]
    public class CubicBezierPath {
        public ControlPoint[] controlPoints;

        private Matrix4x4[] m_Path;

        public Matrix4x4[] Path => m_Path;

        public CubicBezierPath(ControlPoint[] controlPoints) {
            this.controlPoints = controlPoints;
        }

        public void CreateUniformPath(int resolution, float sampleDistance, float stepDistance = 0.1f) {
            var nonUniformPath = ComputeNonUniformPath(resolution);
            // var uniformPath = ComputeUniformPath(nonUniformPath, sampleDistance, stepDistance);
            m_Path = nonUniformPath;

            //TODO: add last node
        }

        public Matrix4x4 GetTRSMatrixAt(float distance) {
            float counter = 0f;
            int currentIndex = 0;
            while (counter < distance) {
                if (currentIndex >= m_Path.Length - 1) {
                    return m_Path[m_Path.Length - 1];
                }

                counter += Vector3.Distance(m_Path[currentIndex].GetPosition(), m_Path[currentIndex + 1].GetPosition());
                currentIndex++;
            }

            if (currentIndex == 0) return m_Path[0];

            var lastMatrix = m_Path[currentIndex - 1];
            var nextMatrix = m_Path[currentIndex];
            var lastPos = m_Path[currentIndex - 1].GetPosition();
            var nextPos = m_Path[currentIndex].GetPosition();
            var lastRotation = m_Path[currentIndex - 1].rotation;
            var nextRotation = m_Path[currentIndex].rotation;
            Vector3 lastScale;
            lastScale.x = new Vector4(lastMatrix.m00, lastMatrix.m10, lastMatrix.m20, lastMatrix.m30).magnitude;
            lastScale.y = new Vector4(lastMatrix.m01, lastMatrix.m11, lastMatrix.m21, lastMatrix.m31).magnitude;
            lastScale.z = new Vector4(lastMatrix.m02, lastMatrix.m12, lastMatrix.m22, lastMatrix.m32).magnitude;
            Vector3 nextScale;
            nextScale.x = new Vector4(nextMatrix.m00, nextMatrix.m10, nextMatrix.m20, nextMatrix.m30).magnitude;
            nextScale.y = new Vector4(nextMatrix.m01, nextMatrix.m11, nextMatrix.m21, nextMatrix.m31).magnitude;
            nextScale.z = new Vector4(nextMatrix.m02, nextMatrix.m12, nextMatrix.m22, nextMatrix.m32).magnitude;

            var t = (counter - distance) / Vector3.Distance(lastPos, nextPos);
            var currentPos = Vector3.Lerp(lastPos, nextPos, t);
            var currentRotation = Quaternion.Lerp(lastRotation, nextRotation, t);
            var currentScale = Vector3.Lerp(lastScale, nextScale, t);
            return Matrix4x4.TRS(currentPos, currentRotation, currentScale);
        }

        private Matrix4x4[] ComputeNonUniformPath(int resolution) {
            var nonUniformPath = new List<Matrix4x4>();
            for (int i = 0; i < controlPoints.Length - 1; i++) {
                var startPos = controlPoints[i].knot;
                var endPos = controlPoints[i + 1].knot;
                var handle1 = controlPoints[i].rightTangent;
                var handle2 = controlPoints[i + 1].leftTangent;
                var startRadius = controlPoints[i].radius;
                var endRadius = controlPoints[i + 1].radius;
                var startTilt = controlPoints[i].tilt;
                var endTilt = controlPoints[i + 1].tilt;

                var pos = startPos;
                for (int j = 1; j <= resolution; j++) {
                    var t = (float)j / resolution;
                    var nextPos = GetPosition(startPos, endPos, handle1, handle2, t);
                    var tiltRotation = GetTiltRotation(startTilt, endTilt, t);
                    var scale = GetScale(startRadius, endRadius, t);
                    Matrix4x4 node = Matrix4x4.TRS(nextPos, Quaternion.LookRotation(nextPos - pos) * tiltRotation, scale);
                    pos = nextPos;
                    nonUniformPath.Add(node);
                }
            }

            nonUniformPath.Add(Matrix4x4.TRS(controlPoints[controlPoints.Length - 1].knot, quaternion.identity, Vector3.one));
            return nonUniformPath.ToArray();
        }

        private Matrix4x4[] ComputeUniformPath(Matrix4x4[] nonUniformPath, float sampleDistance, float stepDistance = 0.05f) {
            if (nonUniformPath.Length < 2) {
                return null;
            }

            var uniformPath = new List<Matrix4x4>();

            var lastPos = nonUniformPath[0].GetPosition();
            var nextPos = nonUniformPath[1].GetPosition();
            int currentId = 0;
            var travelled = 0f;
            var travelledLocal = 0f;
            while (currentId < nonUniformPath.Length - 1) {
                travelled += stepDistance;
                travelledLocal += stepDistance;
                var deltaDistance = Vector3.Distance(lastPos, nextPos);
                if (travelledLocal >= deltaDistance) {
                    currentId++;
                    travelledLocal -= deltaDistance;
                }

                var currentPos = Vector3.Lerp(nonUniformPath[currentId].GetPosition(), nonUniformPath[currentId + 1].GetPosition(), travelled / sampleDistance);

                if (travelled >= sampleDistance) {
                    var t = travelledLocal / deltaDistance;
                    //add node
                    // var rotation = Quaternion.LookRotation(nextPos - lastPos, Vector3.up) *
                    //                GetTiltRotation(controlPoints[currentId], controlPoints[currentId + 1], t);
                    // var scale = GetScale(controlPoints[currentId].radius, controlPoints[currentId + 1].radius, t);
                    Matrix4x4 node = Matrix4x4.TRS(currentPos, quaternion.identity, Vector3.one);
                    uniformPath.Add(node);
                    travelled = 0;
                }
            }

            return uniformPath.ToArray();
        }

        private Vector3 GetPosition(Vector3 start, Vector3 end, Vector3 handle1, Vector3 handle2, float t) {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * start;
            p += 3 * uu * t * handle1;
            p += 3 * u * tt * handle2;
            p += ttt * end;

            return p;
        }

        private Quaternion GetTiltRotation(float startTilt, float endTilt, float t) {
            var tilt = Mathf.Lerp(startTilt, endTilt, t);
            var rotation = Quaternion.Euler(0, 0, tilt);
            return rotation;
        }

        private Vector3 GetScale(float startRadius, float endRadius, float t) {
            var scale = Mathf.Lerp(startRadius, endRadius, t);
            return new Vector3(scale, scale, scale);
        }
    }
}
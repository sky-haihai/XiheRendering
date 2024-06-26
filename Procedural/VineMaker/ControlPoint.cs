using System;
using UnityEngine;

namespace XiheRendering.Procedural.VineMaker.Scripts {
    [Serializable]
    public class ControlPoint {
        public Vector3 knot;
        public Vector3 leftTangent;
        public Vector3 rightTangent;
        public float radius = 1f;
        public float tilt = 0f;

        public ControlPoint() { }

        public ControlPoint(Vector3 knot, float radius = 1f, float tilt = 0f) {
            this.knot = knot;
            this.leftTangent = knot + Vector3.left * 10f;
            this.rightTangent = knot + Vector3.right * 10f;
            this.radius = radius;
            this.tilt = tilt;
        }

        public ControlPoint(Vector3 knot, Vector3 leftTangent, Vector3 rightTangent, float radius = 1f, float tilt = 0f) {
            this.knot = knot;
            this.leftTangent = leftTangent;
            this.rightTangent = rightTangent;
            this.radius = radius;
            this.tilt = tilt;
        }
    }
}
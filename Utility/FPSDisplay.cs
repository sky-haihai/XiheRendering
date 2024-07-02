using UnityEngine;

namespace XiheRendering {
    public class FPSDisplay : MonoBehaviour {
        public float delay = 1.0f;

        private float m_DeltaTime = 0.0f;
        private float m_Timer = 0.0f;

        void Update() {
            m_Timer += Time.deltaTime;
            if (m_Timer > delay) {
                m_DeltaTime = Time.deltaTime;
                m_Timer -= delay;
            }
        }

        void OnGUI() {
            int w = Screen.width, h = Screen.height;

            GUIStyle style = new GUIStyle();

            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);

            float msec = m_DeltaTime * 1000.0f;
            float fps = 1.0f / m_DeltaTime;
            string text = $"{msec:0.0} ms ({fps:0.} fps)";
            GUI.Label(rect, text, style);
        }
    }
}
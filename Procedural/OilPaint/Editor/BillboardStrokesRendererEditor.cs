namespace XiheRendering.Procedural.OilPaint.Editor {
    [UnityEditor.CustomEditor(typeof(BillboardStrokesRenderer))]
    public class BillboardStrokesRendererEditor : UnityEditor.Editor {
        private BillboardStrokesRenderer m_Target;

        private void OnEnable() {
            m_Target = (BillboardStrokesRenderer)target;
        }

        public override void OnInspectorGUI() {
            base.OnInspectorGUI();
            
            
        }
    }
}
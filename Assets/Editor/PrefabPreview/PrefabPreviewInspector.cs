using UnityEditor;
using UnityEngine;

namespace PrefabPreview.Editor
{
    [CustomPreview(typeof(GameObject))]
    public class PrefabPreviewInspector : ObjectPreview
    {
        private readonly PrefabPreviewRenderer m_Renderer = new PrefabPreviewRenderer();
        private int m_TargetId;
        private static readonly int s_ControlId = "PrefabPreviewInspector".GetHashCode();

        public override bool HasPreviewGUI()
        {
            GameObject gameObject = target as GameObject;
            return gameObject != null && PrefabUtility.IsPartOfPrefabAsset(gameObject);
        }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            GameObject gameObject = target as GameObject;
            if (gameObject == null || !HasPreviewGUI()) return;

            int id = gameObject.GetInstanceID();
            if (id != m_TargetId || !m_Renderer.IsLoaded(gameObject))
            {
                m_Renderer.Load(gameObject);
                m_TargetId = id;
            }

            int controlId = GUIUtility.GetControlID(s_ControlId, FocusType.Passive, rect);
            m_Renderer.HandleInput(rect, controlId);
            m_Renderer.Draw(rect, background);
        }

        public override string GetInfoString()
        {
            return m_Renderer.Info;
        }

        public override void Cleanup()
        {
            m_Renderer.Release();
            base.Cleanup();
        }
    }
}

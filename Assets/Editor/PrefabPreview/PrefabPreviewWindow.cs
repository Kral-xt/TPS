using UnityEditor;
using UnityEngine;

namespace PrefabPreview.Editor
{
    public class PrefabPreviewWindow : EditorWindow
    {
        private readonly PrefabPreviewRenderer m_Renderer = new PrefabPreviewRenderer();
        private readonly string[] m_CanvasOptions = { "竖屏 1080x1920", "横屏 1920x1080" };
        private readonly Vector2[] m_CanvasSizes = { new Vector2(1080f, 1920f), new Vector2(1920f, 1080f) };
        private int m_CanvasPresetIndex;
        private GameObject m_CurrentPrefabAsset;

        [MenuItem("Tools/Prefab Preview")]
        public static void ShowWindow()
        {
            PrefabPreviewWindow window = GetWindow<PrefabPreviewWindow>("Prefab Preview");
            window.minSize = new Vector2(300f, 300f);
            window.Show();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            UpdateFromSelection();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            m_Renderer.Release();
        }

        private void OnSelectionChanged()
        {
            UpdateFromSelection();
            Repaint();
        }

        private void UpdateFromSelection()
        {
            GameObject prefabAsset = TryGetPrefabAsset(Selection.activeObject as GameObject);
            if (prefabAsset == null)
            {
                m_Renderer.Release();
                m_CurrentPrefabAsset = null;
                return;
            }

            if (prefabAsset != m_CurrentPrefabAsset || !m_Renderer.IsLoaded(prefabAsset))
            {
                m_Renderer.Load(prefabAsset);
                m_CurrentPrefabAsset = prefabAsset;
            }
        }

        private GameObject TryGetPrefabAsset(GameObject gameObject)
        {
            if (gameObject == null) return null;

            string path = AssetDatabase.GetAssetPath(gameObject);
            if (!string.IsNullOrEmpty(path) && PrefabUtility.GetPrefabAssetType(gameObject) != PrefabAssetType.NotAPrefab)
                return gameObject;

            if (PrefabUtility.IsPartOfPrefabInstance(gameObject))
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                if (source != null) return TryGetPrefabAsset(source);
            }

            if (PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                if (root != null) return TryGetPrefabAsset(root);
            }

            return null;
        }

        private void OnGUI()
        {
            if (!m_Renderer.HasPreview)
            {
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("请在 Project 中选择一个 Prefab", EditorStyles.largeLabel);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
                return;
            }

            EditorGUILayout.LabelField("Prefab: " + m_Renderer.PrefabName, EditorStyles.boldLabel);
            if (m_Renderer.IsUI)
            {
                EditorGUI.BeginChangeCheck();
                int nextPreset = EditorGUILayout.Popup("UI画布", m_CanvasPresetIndex, m_CanvasOptions);
                if (EditorGUI.EndChangeCheck())
                {
                    m_CanvasPresetIndex = nextPreset;
                    m_Renderer.SetReferenceResolution(m_CanvasSizes[m_CanvasPresetIndex]);
                    Repaint();
                }
            }
            EditorGUILayout.LabelField(m_Renderer.Info, EditorStyles.miniLabel);

            Rect rect = GUILayoutUtility.GetRect(position.width - 20f, position.height - 50f);
            if (rect.width <= 1f || rect.height <= 1f)
                return;

            int controlId = GUIUtility.GetControlID("PrefabPreviewWindow".GetHashCode(), FocusType.Passive, rect);
            m_Renderer.HandleInput(rect, controlId);
            m_Renderer.Draw(rect, GUIStyle.none);
        }
    }
}



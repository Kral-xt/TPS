using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace PrefabPreview.Editor
{
    internal sealed class PrefabPreviewRenderer
    {
        private const int UILayer = 5;
        private static readonly Vector2 DefaultCanvasSize = new Vector2(1080f, 1920f);

        private PreviewRenderUtility m_Preview;
        private Material m_FallbackMaterial;
        private GameObject m_Prefab;
        private GameObject m_UIHost;
        private Camera m_UICamera;
        private RenderTexture m_UITexture;
        private Scene m_PreviewScene;
        private readonly List<SkinCache> m_SkinCaches = new List<SkinCache>();

        private Bounds m_ModelBounds;
        private Rect m_UIBounds;
        private Vector2 m_UISize = Vector2.one;
        private string m_PrefabPath = string.Empty;
        private string m_PrefabName = string.Empty;
        private bool m_IsUI;
        private float m_Yaw;
        private float m_Pitch;
        private float m_Zoom = 1.2f;
        
        private Vector2 m_UIPan;
private Vector2 m_ReferenceResolution = DefaultCanvasSize;
        private GUIStyle m_TextPreviewStyle;

        private struct SkinCache
        {
            public SkinnedMeshRenderer Renderer;
            public Mesh Mesh;
        }

        public bool HasPreview { get { return m_Prefab != null; } }
        public bool IsUI { get { return m_IsUI; } }
        public string PrefabName { get { return m_PrefabName; } }
        public string Info
        {
            get
            {
                if (!HasPreview) return string.Empty;
                return m_IsUI
                    ? string.Format("UI {0:F0}x{1:F0} zoom {2:F2}", m_UISize.x, m_UISize.y, m_Zoom)
                    : string.Format("Bounds: {0:F2} zoom {1:F2}", m_ModelBounds.size, m_Zoom);
            }
        }

        public bool IsLoaded(GameObject prefabAsset)
        {
            return prefabAsset != null && AssetDatabase.GetAssetPath(prefabAsset) == m_PrefabPath && m_Prefab != null;
        }

        public void SetReferenceResolution(Vector2 referenceResolution)
        {
            if (referenceResolution.x <= 0f || referenceResolution.y <= 0f)
                return;

            if (Mathf.Approximately(m_ReferenceResolution.x, referenceResolution.x)
                && Mathf.Approximately(m_ReferenceResolution.y, referenceResolution.y))
                return;

            m_ReferenceResolution = referenceResolution;
            RebuildUIPreviewHost();
        }

        private void RebuildUIPreviewHost()
        {
            if (!m_IsUI || m_Prefab == null)
                return;

            if (m_UIHost != null)
            {
                m_Prefab.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(m_UIHost);
                m_UIHost = null;
            }

            SetupUIPreview();
        }
        public void Load(GameObject prefabAsset)
        {
            if (prefabAsset == null) return;

            string path = AssetDatabase.GetAssetPath(prefabAsset);
            if (string.IsNullOrEmpty(path)) return;

            ClearLoadedPrefab();
            EnsurePreviewScene();

            m_Prefab = PrefabUtility.InstantiatePrefab(prefabAsset, m_PreviewScene) as GameObject;
            if (m_Prefab == null) return;
            m_Prefab.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m_Prefab.transform.localScale = Vector3.one;

            m_PrefabPath = path;
            m_PrefabName = prefabAsset.name;
            m_Yaw = 0f;
            m_Pitch = 0f;
            m_Zoom = 1.2f;
            m_UIPan = Vector2.zero;

            m_IsUI = m_Prefab.GetComponentInChildren<RectTransform>(true) != null
                  || m_Prefab.GetComponentInChildren<Graphic>(true) != null
                  || m_Prefab.GetComponentInChildren<CanvasRenderer>(true) != null;

            if (m_IsUI)
            {
                SetupUIPreview();
            }
            else
            {
                SetupModelPreview();
            }
        }

        public void Draw(Rect rect, GUIStyle background)
        {
            if (!HasPreview) return;
            if (m_IsUI) DrawUI(rect);
            else DrawModel(rect, background);
        }

public void HandleInput(Rect rect, int controlId)
        {
            Event ev = Event.current;
            switch (ev.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (ev.button == 0 && rect.Contains(ev.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        ev.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (ev.button == 0 && GUIUtility.hotControl == controlId)
                    {
                        if (m_IsUI && m_UICamera != null)
                        {
                            float worldUnitsPerPixel = m_UICamera.orthographicSize * 2f
                                / Mathf.Max(1f, rect.height);
                            m_UIPan.x -= ev.delta.x * worldUnitsPerPixel;
                            m_UIPan.y += ev.delta.y * worldUnitsPerPixel;
                        }
                        else
                        {
                            m_Yaw += ev.delta.x * 0.5f;
                            m_Pitch -= ev.delta.y * 0.5f;
                            m_Pitch = Mathf.Clamp(m_Pitch, -80f, 80f);
                        }
                        ev.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (ev.button == 0 && GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        ev.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (rect.Contains(ev.mousePosition))
                    {
                        m_Zoom += ev.delta.y * 0.05f;
                        m_Zoom = Mathf.Clamp(m_Zoom, 0.1f, 10f);
                        ev.Use();
                    }
                    break;
            }
        }

        public void Release()
        {
            ClearLoadedPrefab();

            if (m_UITexture != null)
            {
                if (m_UICamera != null && m_UICamera.targetTexture == m_UITexture)
                    m_UICamera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(m_UITexture);
                m_UITexture = null;
            }

            if (m_UICamera != null)
            {
                UnityEngine.Object.DestroyImmediate(m_UICamera.gameObject);
                m_UICamera = null;
            }

            if (m_Preview != null)
            {
                m_Preview.Cleanup();
                m_Preview = null;
            }

            if (m_FallbackMaterial != null)
            {
                UnityEngine.Object.DestroyImmediate(m_FallbackMaterial);
                m_FallbackMaterial = null;
            }

            if (m_PreviewScene.IsValid())
            {
                EditorSceneManager.ClosePreviewScene(m_PreviewScene);
                m_PreviewScene = default(Scene);
            }
        }

        private void ClearLoadedPrefab()
        {
            foreach (SkinCache cache in m_SkinCaches)
            {
                if (cache.Mesh != null)
                    UnityEngine.Object.DestroyImmediate(cache.Mesh);
            }
            m_SkinCaches.Clear();

            if (m_Prefab != null)
            {
                UnityEngine.Object.DestroyImmediate(m_Prefab);
                m_Prefab = null;
            }

            if (m_UIHost != null)
            {
                UnityEngine.Object.DestroyImmediate(m_UIHost);
                m_UIHost = null;
            }

            m_PrefabPath = string.Empty;
            m_PrefabName = string.Empty;
            m_UIBounds = new Rect(-0.5f, -0.5f, 1f, 1f);
            m_UISize = Vector2.one;
            m_ModelBounds = new Bounds(Vector3.zero, Vector3.one);
        }

        private void EnsurePreviewScene()
        {
            if (!m_PreviewScene.IsValid())
                m_PreviewScene = EditorSceneManager.NewPreviewScene();
        }

        private void EnsureModelPreview()
        {
            if (m_Preview != null) return;
            m_Preview = new PreviewRenderUtility();
            m_FallbackMaterial = new Material(Shader.Find("Standard"))
            {
                color = Color.magenta,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

private void EnsureUICamera(int width, int height)
        {
            if (m_UICamera == null)
            {
                GameObject cameraObject = new GameObject("PrefabPreview_UICamera", typeof(Camera));
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                SceneManager.MoveGameObjectToScene(cameraObject, SceneManager.GetActiveScene());
                m_UICamera = cameraObject.GetComponent<Camera>();
                m_UICamera.orthographic = true;
                m_UICamera.clearFlags = CameraClearFlags.SolidColor;
                m_UICamera.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
                m_UICamera.cullingMask = 1 << UILayer;
                m_UICamera.nearClipPlane = 0.01f;
                m_UICamera.farClipPlane = 100f;
            }

            if (m_UITexture == null || m_UITexture.width != width || m_UITexture.height != height)
            {
                if (m_UITexture != null)
                {
                    if (m_UICamera.targetTexture == m_UITexture)
                        m_UICamera.targetTexture = null;
                    UnityEngine.Object.DestroyImmediate(m_UITexture);
                }

                m_UITexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                m_UICamera.targetTexture = m_UITexture;
            }
        }

        private void SetupModelPreview()
        {
            Renderer[] renderers = m_Prefab.GetComponentsInChildren<Renderer>(true);
            m_ModelBounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.one);
            for (int i = 1; i < renderers.Length; i++)
                m_ModelBounds.Encapsulate(renderers[i].bounds);

            foreach (SkinnedMeshRenderer renderer in m_Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (renderer.sharedMesh == null) continue;
                Mesh bakedMesh = new Mesh();
                renderer.BakeMesh(bakedMesh);
                m_SkinCaches.Add(new SkinCache { Renderer = renderer, Mesh = bakedMesh });
            }
        }

private void SetupUIPreview()
        {
            EnsureUICamera(1, 1);

            Vector2 canvasSize = GetCanvasSize(m_Prefab);
            m_UIHost = new GameObject("PrefabPreview_UIHost", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            m_UIHost.hideFlags = HideFlags.HideAndDontSave;
            Scene activeScene = SceneManager.GetActiveScene();
            SceneManager.MoveGameObjectToScene(m_UIHost, activeScene);

            RectTransform hostRect = m_UIHost.GetComponent<RectTransform>();
            hostRect.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            hostRect.localScale = Vector3.one;
            hostRect.anchorMin = Vector2.zero;
            hostRect.anchorMax = Vector2.one;
            hostRect.pivot = new Vector2(0.5f, 0.5f);
            hostRect.sizeDelta = canvasSize;

            Canvas hostCanvas = m_UIHost.GetComponent<Canvas>();
            hostCanvas.renderMode = RenderMode.WorldSpace;
            hostCanvas.worldCamera = m_UICamera;
            hostCanvas.sortingOrder = 0;

            CanvasScaler hostScaler = m_UIHost.GetComponent<CanvasScaler>();
            hostScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            hostScaler.scaleFactor = 1f;
            hostScaler.referencePixelsPerUnit = 100f;

            RectTransform prefabRect = m_Prefab.transform as RectTransform;
            if (prefabRect != null)
            {
                SceneManager.MoveGameObjectToScene(m_Prefab, activeScene);
                m_Prefab.hideFlags = HideFlags.HideAndDontSave;
                prefabRect.SetParent(hostRect, false);
            }

            ConfigureCanvases(m_Prefab);
            SetLayerRecursive(m_UIHost, UILayer);
            m_UIHost.SetActive(true);
            m_Prefab.SetActive(true);

            RebuildUILayout();
            m_UIBounds = CalculateVisibleUIBounds(hostRect);
            if (m_UIBounds.width < 1f || m_UIBounds.height < 1f)
                m_UIBounds = new Rect(-canvasSize.x * 0.5f, -canvasSize.y * 0.5f, canvasSize.x, canvasSize.y);

            m_UISize = m_UIBounds.size;
        }

        private Vector2 GetCanvasSize(GameObject root)
        {
            RectTransform rootRect = root.transform as RectTransform;
            if (rootRect != null)
            {
                bool stretchX = !Mathf.Approximately(rootRect.anchorMin.x, rootRect.anchorMax.x);
                bool stretchY = !Mathf.Approximately(rootRect.anchorMin.y, rootRect.anchorMax.y);
                if (!stretchX && !stretchY && rootRect.rect.width > 1f && rootRect.rect.height > 1f)
                    return new Vector2(Mathf.Abs(rootRect.rect.width), Mathf.Abs(rootRect.rect.height));
            }

            return m_ReferenceResolution;
        }

        private void ConfigureCanvases(GameObject root)
        {
            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = m_UICamera;
                canvas.overrideSorting = false;
            }
        }

        private void RebuildUILayout()
        {
            Canvas.ForceUpdateCanvases();

            RectTransform[] rects = m_UIHost.GetComponentsInChildren<RectTransform>(true);
            Array.Sort(rects, (a, b) => GetDepth(b.transform).CompareTo(GetDepth(a.transform)));
            foreach (RectTransform rect in rects)
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            Canvas.ForceUpdateCanvases();

            TMP_Text[] tmpTexts = m_UIHost.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in tmpTexts)
                text.ForceMeshUpdate(true, true);

            Canvas.ForceUpdateCanvases();
        }

        private int GetDepth(Transform transform)
        {
            int depth = 0;
            while (transform != null)
            {
                depth++;
                transform = transform.parent;
            }
            return depth;
        }

        private Rect CalculateVisibleUIBounds(RectTransform space)
        {
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            bool hasBounds = false;
            Vector3[] corners = new Vector3[4];

            Graphic[] graphics = m_UIHost.GetComponentsInChildren<Graphic>(true);
            foreach (Graphic graphic in graphics)
            {
                if (graphic == null || !graphic.enabled || !graphic.gameObject.activeInHierarchy)
                    continue;

                if (graphic.color.a * GetCanvasGroupAlpha(graphic.transform) <= 0.001f)
                    continue;

                RectTransform rect = graphic.transform as RectTransform;
                if (rect == null)
                    continue;

                rect.GetWorldCorners(corners);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 local = space.InverseTransformPoint(corners[i]);
                    min = Vector2.Min(min, local);
                    max = Vector2.Max(max, local);
                }
                hasBounds = true;
            }

            if (!hasBounds)
            {
                Rect host = space.rect;
                return new Rect(host.xMin, host.yMin, host.width, host.height);
            }

            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private float GetCanvasGroupAlpha(Transform transform)
        {
            float alpha = 1f;
            Transform current = transform;
            while (current != null)
            {
                CanvasGroup group = current.GetComponent<CanvasGroup>();
                if (group != null)
                    alpha *= group.alpha;
                current = current.parent;
            }
            return alpha;
        }

private void DrawUI(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f, 1f));
            if (m_UIHost == null)
                return;

            RebuildUILayout();
            RectTransform hostRect = m_UIHost.GetComponent<RectTransform>();
            m_UIBounds = CalculateVisibleUIBounds(hostRect);
            if (m_UIBounds.width < 1f || m_UIBounds.height < 1f)
                return;

            m_UISize = m_UIBounds.size;
            int textureWidth = Mathf.Max(1, Mathf.CeilToInt(rect.width));
            int textureHeight = Mathf.Max(1, Mathf.CeilToInt(rect.height));
            EnsureUICamera(textureWidth, textureHeight);

            float aspect = textureWidth / (float)textureHeight;
            float halfHeight = Mathf.Max(
                m_UIBounds.height * 0.5f,
                m_UIBounds.width * 0.5f / Mathf.Max(0.01f, aspect));

            m_UICamera.orthographicSize = Mathf.Max(0.01f, halfHeight / m_Zoom);
            Vector2 cameraCenter = m_UIBounds.center + m_UIPan;
            m_UICamera.transform.SetPositionAndRotation(
                new Vector3(cameraCenter.x, cameraCenter.y, -10f),
                Quaternion.identity);
            m_UICamera.Render();
            GUI.DrawTexture(rect, m_UITexture, ScaleMode.StretchToFill, false);
        }

        private Rect GetGUIRect(RectTransform hostRect, RectTransform targetRect, Rect drawArea, float scale)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            Vector2 min = Vector2.positiveInfinity;
            Vector2 max = Vector2.negativeInfinity;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 local = hostRect.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }

            float x = drawArea.x + (min.x - m_UIBounds.xMin) * scale;
            float y = drawArea.y + (m_UIBounds.yMax - max.y) * scale;
            return new Rect(x, y, (max.x - min.x) * scale, (max.y - min.y) * scale);
        }

        private void DrawImageGraphic(Rect rect, Image image)
        {
            if (image.sprite == null)
            {
                if (image.color.a > 0f)
                    EditorGUI.DrawRect(rect, GUI.color);
                return;
            }

            Texture2D texture = image.sprite.texture;
            if (texture == null)
                return;

            if (image.type == Image.Type.Sliced && image.sprite.border.sqrMagnitude > 0f)
            {
                DrawSlicedSprite(rect, image.sprite);
                return;
            }

            Vector4 outer = DataUtility.GetOuterUV(image.sprite);
            Rect uv = new Rect(outer.x, outer.y, outer.z - outer.x, outer.w - outer.y);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
        }

        private void DrawSlicedSprite(Rect rect, Sprite sprite)
        {
            Texture2D texture = sprite.texture;
            Vector4 outer = DataUtility.GetOuterUV(sprite);
            Vector4 inner = DataUtility.GetInnerUV(sprite);
            Vector4 border = sprite.border;
            Rect spriteRect = sprite.rect;

            float left = Mathf.Min(rect.width * 0.5f, rect.width * (border.x / Mathf.Max(1f, spriteRect.width)));
            float right = Mathf.Min(rect.width * 0.5f, rect.width * (border.z / Mathf.Max(1f, spriteRect.width)));
            float bottom = Mathf.Min(rect.height * 0.5f, rect.height * (border.y / Mathf.Max(1f, spriteRect.height)));
            float top = Mathf.Min(rect.height * 0.5f, rect.height * (border.w / Mathf.Max(1f, spriteRect.height)));

            float[] xs = { rect.xMin, rect.xMin + left, rect.xMax - right, rect.xMax };
            float[] ys = { rect.yMin, rect.yMin + top, rect.yMax - bottom, rect.yMax };
            float[] us = { outer.x, inner.x, inner.z, outer.z };
            float[] vs = { outer.w, inner.w, inner.y, outer.y };

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Rect drawRect = Rect.MinMaxRect(xs[x], ys[y], xs[x + 1], ys[y + 1]);
                    if (drawRect.width <= 0f || drawRect.height <= 0f)
                        continue;

                    Rect uv = Rect.MinMaxRect(us[x], vs[y + 1], us[x + 1], vs[y]);
                    GUI.DrawTextureWithTexCoords(drawRect, texture, uv, true);
                }
            }
        }

        private void DrawTmpTextGraphic(Rect rect, TMP_Text text, Color color, float scale)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
                return;

            EnsureTextStyle();
            m_TextPreviewStyle.alignment = ConvertTmpAlignment(text.alignment);
            m_TextPreviewStyle.font = null;
            m_TextPreviewStyle.fontStyle = text.fontStyle.HasFlag(FontStyles.Bold) ? FontStyle.Bold : FontStyle.Normal;
            m_TextPreviewStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(text.fontSize * scale));
            m_TextPreviewStyle.normal.textColor = color;
            GUI.Label(rect, text.text, m_TextPreviewStyle);
        }

        private void DrawTextGraphic(Rect rect, Text text, Color color, float scale)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
                return;

            EnsureTextStyle();
            m_TextPreviewStyle.alignment = text.alignment;
            m_TextPreviewStyle.font = text.font;
            m_TextPreviewStyle.fontStyle = text.fontStyle;
            m_TextPreviewStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(text.fontSize * scale));
            m_TextPreviewStyle.normal.textColor = color;
            GUI.Label(rect, text.text, m_TextPreviewStyle);
        }

        private void EnsureTextStyle()
        {
            if (m_TextPreviewStyle == null)
                m_TextPreviewStyle = new GUIStyle(EditorStyles.label) { clipping = TextClipping.Clip };
        }

        private TextAnchor ConvertTmpAlignment(TextAlignmentOptions alignment)
        {
            if ((alignment & TextAlignmentOptions.Top) != 0)
            {
                if ((alignment & TextAlignmentOptions.Left) != 0) return TextAnchor.UpperLeft;
                if ((alignment & TextAlignmentOptions.Right) != 0) return TextAnchor.UpperRight;
                return TextAnchor.UpperCenter;
            }
            if ((alignment & TextAlignmentOptions.Bottom) != 0)
            {
                if ((alignment & TextAlignmentOptions.Left) != 0) return TextAnchor.LowerLeft;
                if ((alignment & TextAlignmentOptions.Right) != 0) return TextAnchor.LowerRight;
                return TextAnchor.LowerCenter;
            }
            if ((alignment & TextAlignmentOptions.Left) != 0) return TextAnchor.MiddleLeft;
            if ((alignment & TextAlignmentOptions.Right) != 0) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }
        private void DrawModel(Rect rect, GUIStyle background)
        {
            EnsureModelPreview();
            m_Preview.BeginPreview(rect, background ?? GUIStyle.none);

            Camera camera = m_Preview.camera;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 100f;
            camera.fieldOfView = 30f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f);

            m_Preview.lights[0].transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            m_Preview.lights[0].intensity = 1.2f;

            float size = Mathf.Max(m_ModelBounds.size.magnitude, 0.1f);
            float distance = size * m_Zoom;
            Quaternion modelRotation = Quaternion.Euler(m_Pitch, m_Yaw, 0f);
            Vector3 cameraPosition = new Vector3(0f, 0f, -distance);
            camera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(-cameraPosition, Vector3.up));

            Matrix4x4 rootMatrix = Matrix4x4.TRS(modelRotation * (-m_ModelBounds.center), modelRotation, Vector3.one);
            DrawMeshes(rootMatrix);
            camera.Render();
            m_Preview.EndAndDrawPreview(rect);
        }

        private void DrawMeshes(Matrix4x4 rootMatrix)
        {
            foreach (MeshFilter meshFilter in m_Prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null) continue;
                MeshRenderer meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                Material[] materials = meshRenderer != null ? meshRenderer.sharedMaterials : Array.Empty<Material>();
                Matrix4x4 matrix = rootMatrix * meshFilter.transform.localToWorldMatrix;
                for (int i = 0; i < meshFilter.sharedMesh.subMeshCount; i++)
                    m_Preview.DrawMesh(meshFilter.sharedMesh, matrix, materials.Length > i && materials[i] != null ? materials[i] : m_FallbackMaterial, i);
            }

            foreach (SkinCache cache in m_SkinCaches)
            {
                if (cache.Mesh == null || cache.Renderer == null) continue;
                Material[] materials = cache.Renderer.sharedMaterials;
                Matrix4x4 matrix = rootMatrix * cache.Renderer.transform.localToWorldMatrix;
                for (int i = 0; i < cache.Mesh.subMeshCount; i++)
                    m_Preview.DrawMesh(cache.Mesh, matrix, materials.Length > i && materials[i] != null ? materials[i] : m_FallbackMaterial, i);
            }
        }

        private void SetLayerRecursive(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
                SetLayerRecursive(child.gameObject, layer);
        }
    }
}








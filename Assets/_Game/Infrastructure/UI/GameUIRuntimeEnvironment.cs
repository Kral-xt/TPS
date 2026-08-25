using QFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TPS.Startup.Infrastructure
{
    public static class GameUIRuntimeEnvironment
    {
        public static bool IsReady { get; private set; }

        public static UIRoot Ensure()
        {
            UIRoot root = EnsureRoot();
            EnsureEventSystem();
            IsReady = root != null && root.Canvas != null && root.Common != null;
            return root;
        }

        private static UIRoot EnsureRoot()
        {
            UIRoot existingRoot = Object.FindFirstObjectByType<UIRoot>(FindObjectsInactive.Include);
            if (existingRoot != null)
            {
                ConfigureRoot(existingRoot);
                Object.DontDestroyOnLoad(existingRoot.gameObject);
                return existingRoot;
            }

            GameObject rootObject = new GameObject("UIRoot", typeof(RectTransform));
            rootObject.layer = 5;
            Object.DontDestroyOnLoad(rootObject);

            UIRoot root = rootObject.AddComponent<UIRoot>();
            ConfigureRoot(root);
            return root;
        }

        private static void ConfigureRoot(UIRoot root)
        {
            root.gameObject.SetActive(true);
            root.gameObject.layer = 5;

            Canvas canvas = root.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = root.gameObject.AddComponent<Canvas>();
            }
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = root.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = root.gameObject.AddComponent<GraphicRaycaster>();
            }

            root.Canvas = canvas;
            root.CanvasScaler = scaler;
            root.GraphicRaycaster = raycaster;
            root.Bg = EnsureLayer(root.transform, root.Bg, "Bg");
            root.Common = EnsureLayer(root.transform, root.Common, "Common");
            root.PopUI = EnsureLayer(root.transform, root.PopUI, "PopUI");
            root.CanvasPanel = EnsureLayer(root.transform, root.CanvasPanel, "CanvasPanel");
        }

        private static void EnsureEventSystem()
        {
            EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>(
                FindObjectsInactive.Include);
            if (existingEventSystem != null)
            {
                existingEventSystem.gameObject.SetActive(true);
                return;
            }

            GameObject eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));
            Object.DontDestroyOnLoad(eventSystemObject);
        }

        private static RectTransform EnsureLayer(
            Transform parent,
            RectTransform current,
            string layerName)
        {
            if (current != null)
            {
                current.gameObject.SetActive(true);
                return current;
            }

            RectTransform existing = parent.Find(layerName) as RectTransform;
            return existing != null ? existing : CreateLayer(parent, layerName);
        }

        private static RectTransform CreateLayer(Transform parent, string layerName)
        {
            GameObject layerObject = new GameObject(layerName, typeof(RectTransform));
            layerObject.layer = 5;
            RectTransform rectTransform = layerObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            return rectTransform;
        }
    }
}

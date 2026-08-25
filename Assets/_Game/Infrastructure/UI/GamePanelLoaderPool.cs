using System;
using QFramework;
using UnityEngine;

namespace TPS.Startup.Infrastructure
{
    public sealed class GamePanelLoaderPool : AbstractPanelLoaderPool
    {
        private const string RegistryPath = "GameUI/GameUIPrefabRegistry";
        private static GameUIPrefabRegistry registry;

        public static string RegistryResourcePath => RegistryPath;
        public static bool IsReady => registry != null;

        public static GameObject ResolvePrefab(string prefabName)
        {
            return LoadRegistry()?.Resolve(null, prefabName);
        }

        public static void Install()
        {
            UIKit.Config.PanelLoaderPool = new GamePanelLoaderPool();
            LoadRegistry();
        }

        protected override IPanelLoader CreatePanelLoader()
        {
            return new RegistryPanelLoader();
        }

        private static GameUIPrefabRegistry LoadRegistry()
        {
            if (registry == null)
            {
                registry = Resources.Load<GameUIPrefabRegistry>(RegistryPath);
                if (registry == null)
                {
                    Debug.LogError(
                        $"[GamePanelLoaderPool] Registry load failed. "
                        + $"Path=Resources/{RegistryPath}, Ready={IsReady}, "
                        + "Reason=Resource not found or wrong type.");
                }
            }

            return registry;
        }

        private sealed class RegistryPanelLoader : IPanelLoader
        {
            private GameObject panelPrefab;

            public GameObject LoadPanelPrefab(PanelSearchKeys panelSearchKeys)
            {
                GameUIPrefabRegistry currentRegistry = LoadRegistry();
                panelPrefab = currentRegistry != null
                    ? currentRegistry.Resolve(panelSearchKeys.PanelType, panelSearchKeys.GameObjName)
                    : null;

                if (panelPrefab == null)
                {
                    string panelName = panelSearchKeys.PanelType?.Name
                        ?? panelSearchKeys.GameObjName
                        ?? "Unknown";
                    string reason = currentRegistry == null
                        ? "Registry is unavailable."
                        : "Panel is not registered or prefab reference is missing.";
                    Debug.LogError(
                        $"[GamePanelLoaderPool] Panel load failed. "
                        + $"Panel={panelName}, Path=Resources/{RegistryPath}, "
                        + $"Ready={IsReady}, Reason={reason}");
                }

                return panelPrefab;
            }

            public void LoadPanelPrefabAsync(
                PanelSearchKeys panelSearchKeys,
                Action<GameObject> onPanelPrefabLoad)
            {
                onPanelPrefabLoad?.Invoke(LoadPanelPrefab(panelSearchKeys));
            }

            public void Unload()
            {
                panelPrefab = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            registry = null;
        }
    }
}

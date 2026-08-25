#if TPS_ENABLE_COMPANY_PACKAGES
using UnityEngine;
using QFramework;

namespace TPS.UI
{
    /// <summary>
    /// 自定义 PanelLoaderPool，使用 AssetDatabase 从指定路径加载 BattleView prefab
    /// </summary>
    public class BattleViewPanelLoaderPool : AbstractPanelLoaderPool
    {
        public class BattleViewPanelLoader : IPanelLoader
        {
            private const string PrefabPath = "Assets/XGameAssets/Modules/Battle/Prefabs/Views/BattleView.prefab";
            private GameObject mPanelPrefab;

            public GameObject LoadPanelPrefab(PanelSearchKeys panelSearchKeys)
            {
#if UNITY_EDITOR
                if (mPanelPrefab == null)
                {
                    mPanelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                    if (mPanelPrefab == null)
                    {
                        Debug.LogError("[BattleViewPanelLoader] FAILED to load prefab: " + PrefabPath);
                    }
                    else
                    {
                        Debug.Log("[BattleViewPanelLoader] Prefab loaded: " + PrefabPath);
                    }
                }
#endif
                if (mPanelPrefab == null)
                {
                    Debug.LogError("[BattleViewPanelLoader] Returning null prefab!");
                }
                return mPanelPrefab;
            }

            public void LoadPanelPrefabAsync(PanelSearchKeys panelSearchKeys, System.Action<GameObject> onPanelLoad)
            {
#if UNITY_EDITOR
                if (mPanelPrefab == null)
                {
                    mPanelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                    if (mPanelPrefab == null)
                    {
                        Debug.LogError("[BattleViewPanelLoader] FAILED to load prefab: " + PrefabPath);
                    }
                    else
                    {
                        Debug.Log("[BattleViewPanelLoader] Prefab loaded: " + PrefabPath);
                    }
                }
#endif
                onPanelLoad?.Invoke(mPanelPrefab);
            }

            public void Unload()
            {
                mPanelPrefab = null;
            }
        }

        protected override IPanelLoader CreatePanelLoader()
        {
            return new BattleViewPanelLoader();
        }
    }
}
#endif

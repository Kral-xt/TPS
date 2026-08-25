using TPS.Player.Presentation;
using TPS.Startup.Infrastructure;
using UnityEngine;
using QFramework;

namespace TPS.Battle
{
    /// <summary>战斗运行时引导，配置 PanelLoader 并打开 BattleView</summary>
    public class BattleRuntimeBootstrap : MonoBehaviour
    {
        private bool mPanelOpened;

        private void Awake()
        {
            Debug.Log("[BattleRuntimeBootstrap] Awake");
            GameAudioManager.EnsureRuntimeInstance();
            GameUIRuntimeEnvironment.Ensure();
            GamePanelLoaderPool.Install();
            CursorStateController cursorController =
                CursorStateController.EnsureRuntimeInstance();
            cursorController.SetEscapeToggleEnabled(true);
            PlayerInputGate.SetGameplay();
        }

        private void Start()
        {
            OpenPanel();
        }

        private void OpenPanel()
        {
            if (mPanelOpened) return;

            try
            {
                Debug.Log("[BattleRuntimeBootstrap] Calling UIKit.OpenPanel<BattleView>");
                var panel = UIKit.OpenPanel<QFramework.Example.BattleView>(UILevel.Common);

                if (panel == null)
                {
                    Debug.LogError(
                        "[BattleRuntimeBootstrap] Panel open failed. "
                        + "Panel=BattleView, "
                        + $"Path=Resources/{GamePanelLoaderPool.RegistryResourcePath}, "
                        + $"UIReady={GameUIRuntimeEnvironment.IsReady}, "
                        + $"LoaderReady={GamePanelLoaderPool.IsReady}, "
                        + "Reason=UIKit.OpenPanel returned null.",
                        this);
                    return;
                }

                mPanelOpened = true;

                Debug.Log("[BattleRuntimeBootstrap] Panel opened: " + panel.name);

                if (panel.Transform != null)
                {
                    Transform p = panel.Transform.parent;
                    string hierarchy = panel.name;
                    while (p != null) { hierarchy = p.name + " / " + hierarchy; p = p.parent; }
                    Debug.Log("[BattleRuntimeBootstrap] Panel hierarchy: " + hierarchy);

                    var rt = panel.Transform as RectTransform;
                    if (rt != null)
                    {
                        Debug.Log("[BattleRuntimeBootstrap] RT anchors: " + rt.anchorMin + " / " + rt.anchorMax
                            + " size: " + rt.sizeDelta + " scale: " + rt.localScale);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex, this);
                Debug.LogError(
                    "[BattleRuntimeBootstrap] Panel open failed. "
                    + "Panel=BattleView, "
                    + $"Path=Resources/{GamePanelLoaderPool.RegistryResourcePath}, "
                    + $"UIReady={GameUIRuntimeEnvironment.IsReady}, "
                    + $"LoaderReady={GamePanelLoaderPool.IsReady}, "
                    + $"Reason={ex.Message}",
                    this);
            }
        }
    }
}

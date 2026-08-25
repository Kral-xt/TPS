using QFramework;
using QFramework.Example;
using TPS.Player.Presentation;
using TPS.Startup.Infrastructure;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class GMViewController : MonoBehaviour
    {
        private static GMViewController instance;

        private GMView currentView;
        private bool isOpening;
        private bool isClosing;
        private bool hasOpenView;

        public static GMViewController Instance => instance;
        public bool IsOpen => currentView != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EnsureRuntimeInstance();
        }

        public static GMViewController EnsureRuntimeInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<GMViewController>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.gameObject.SetActive(true);
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            GameObject controllerObject = new GameObject(nameof(GMViewController));
            DontDestroyOnLoad(controllerObject);
            instance = controllerObject.AddComponent<GMViewController>();
            return instance;
        }

        public void OpenGM()
        {
            if (currentView != null || isOpening || isClosing || !PlayerInputGate.IsGameplay)
            {
                return;
            }

            isOpening = true;
            try
            {
                GameUIRuntimeEnvironment.Ensure();
                GamePanelLoaderPool.Install();

                GMView view = UIKit.OpenPanel<GMView>(UILevel.PopUI);
                if (view == null)
                {
                    Debug.LogError("[GMViewController] 打开 GM 页面失败：UIKit.OpenPanel 返回空。", this);
                    return;
                }

                currentView = view;
                hasOpenView = true;
                PlayerInputGate.SetUI();
                view.Setup(this);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                if (currentView != null)
                {
                    GMView view = currentView;
                    currentView = null;
                    hasOpenView = false;
                    UIKit.ClosePanel(view);
                }

                PlayerInputGate.SetGameplay();
            }
            finally
            {
                isOpening = false;
            }
        }

        public void CloseGM()
        {
            if (currentView == null || isClosing)
            {
                return;
            }

            isClosing = true;
            PlayerInputGate.SetUI();
            UIKit.ClosePanel(currentView);
        }

        public void AddItem(int itemID, int count)
        {
            PlayerInventoryController inventory = PlayerInventoryController.Instance;
            if (inventory == null)
            {
                Debug.LogError("[GMViewController] 添加物品失败：玩家背包尚未初始化。", this);
                return;
            }

            if (!inventory.HasItemConfig(itemID))
            {
                Debug.LogWarning(
                    $"[GMViewController] 添加物品失败：未找到 ItemConfig。ItemID={itemID}",
                    this);
                return;
            }

            int previousEntryCount = inventory.EntryCount;
            inventory.AddItem(itemID, count);
            if (inventory.EntryCount == previousEntryCount)
            {
                return;
            }

            inventory.SaveInventory();
            BagDialogController.Instance?.RefreshBag();
        }

        public void NotifyViewClosed(GMView view)
        {
            if (currentView != view)
            {
                return;
            }

            currentView = null;
            hasOpenView = false;
            isClosing = false;
            PlayerInputGate.SetGameplay();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (hasOpenView && currentView == null)
            {
                hasOpenView = false;
                isClosing = false;
                PlayerInputGate.SetGameplay();
            }

            if (currentView != null)
            {
                if (Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseGM();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                OpenGM();
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
        }
    }
}

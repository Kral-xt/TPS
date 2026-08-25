using QFramework;
using QFramework.Example;
using TPS.Player.Presentation;
using TPS.Startup.Infrastructure;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class BagDialogController : MonoBehaviour
    {
        private static BagDialogController instance;

        private BagDialog currentDialog;
        private ItemCellPool itemCellPool;
        private bool isOpening;
        private bool isClosing;
        private bool hasOpenDialog;

        public static BagDialogController Instance => instance;
        public bool IsOpen => currentDialog != null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            EnsureRuntimeInstance();
        }

        public static BagDialogController EnsureRuntimeInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<BagDialogController>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.gameObject.SetActive(true);
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            GameObject controllerObject = new GameObject(nameof(BagDialogController));
            DontDestroyOnLoad(controllerObject);
            instance = controllerObject.AddComponent<BagDialogController>();
            return instance;
        }

        public void OpenBag()
        {
            if (currentDialog != null || isOpening || isClosing || !PlayerInputGate.IsGameplay)
            {
                return;
            }

            isOpening = true;
            try
            {
                GameUIRuntimeEnvironment.Ensure();
                GamePanelLoaderPool.Install();

                GameObject bagDialogPrefab = GamePanelLoaderPool.ResolvePrefab("BagDialog");
                GameObject itemCellPrefab = GamePanelLoaderPool.ResolvePrefab("ItemCell");
                RectTransform prefabLayout = bagDialogPrefab != null
                    ? bagDialogPrefab.transform as RectTransform
                    : null;
                if (prefabLayout == null || itemCellPrefab == null)
                {
                    throw new System.InvalidOperationException(
                        "GameUIPrefabRegistry 未完整注册 BagDialog 或 ItemCell Prefab。");
                }

                if (itemCellPool == null)
                {
                    itemCellPool = new ItemCellPool(itemCellPrefab, transform);
                }

                BagDialog dialog = UIKit.OpenPanel<BagDialog>(UILevel.PopUI);
                if (dialog == null)
                {
                    Debug.LogError(
                        "[BagDialogController] 打开背包失败：UIKit.OpenPanel 返回空。",
                        this);
                    return;
                }

                currentDialog = dialog;
                hasOpenDialog = true;
                PlayerInputGate.SetUI();
                dialog.Setup(this, itemCellPool, prefabLayout);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                if (currentDialog != null)
                {
                    BagDialog dialog = currentDialog;
                    currentDialog = null;
                    hasOpenDialog = false;
                    UIKit.ClosePanel(dialog);
                }

                PlayerInputGate.SetGameplay();
            }
            finally
            {
                isOpening = false;
            }
        }

        public void CloseBag()
        {
            if (currentDialog == null || isClosing)
            {
                return;
            }

            isClosing = true;
            PlayerInputGate.SetUI();
            currentDialog.BeginClose(OnHideAnimationCompleted);
        }

        public void RefreshBag()
        {
            currentDialog?.RefreshBag();
        }

        public void NotifyDialogClosed(BagDialog dialog)
        {
            if (currentDialog != dialog)
            {
                return;
            }

            currentDialog = null;
            hasOpenDialog = false;
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
            if (hasOpenDialog && currentDialog == null)
            {
                hasOpenDialog = false;
                isClosing = false;
                PlayerInputGate.SetGameplay();
            }

            if (currentDialog != null)
            {
                if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.Escape))
                {
                    CloseBag();
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                OpenBag();
            }
        }

        private void OnHideAnimationCompleted()
        {
            if (currentDialog == null)
            {
                isClosing = false;
                PlayerInputGate.SetGameplay();
                return;
            }

            BagDialog dialog = currentDialog;
            currentDialog = null;
            hasOpenDialog = false;
            isClosing = false;
            UIKit.ClosePanel(dialog);
            PlayerInputGate.SetGameplay();
        }

        private void OnDestroy()
        {
            itemCellPool?.Dispose();
            itemCellPool = null;

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

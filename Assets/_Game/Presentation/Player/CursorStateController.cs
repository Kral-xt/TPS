using UnityEngine;

namespace TPS.Player.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CursorStateController : MonoBehaviour
    {
        private static CursorStateController instance;

        private bool hasFocus = true;
        private bool isApplicationPaused;
        private bool escapeToggleEnabled;

        public static CursorStateController EnsureRuntimeInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<CursorStateController>(FindObjectsInactive.Include);
            if (instance != null)
            {
                instance.gameObject.SetActive(true);
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            GameObject controllerObject = new GameObject(nameof(CursorStateController));
            DontDestroyOnLoad(controllerObject);
            instance = controllerObject.AddComponent<CursorStateController>();
            return instance;
        }

        public void SetEscapeToggleEnabled(bool enabled)
        {
            escapeToggleEnabled = enabled;
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

        private void OnEnable()
        {
            PlayerInputGate.ModeChanged += OnInputModeChanged;
            ApplyCursorState(true);
        }

        private void OnDisable()
        {
            PlayerInputGate.ModeChanged -= OnInputModeChanged;
        }

        private void Update()
        {
            if (!escapeToggleEnabled || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            if (PlayerInputGate.IsGameplay)
            {
                PlayerInputGate.SetUI();
            }
            else
            {
                PlayerInputGate.SetGameplay();
            }
        }

        private void OnInputModeChanged(PlayerInputMode mode)
        {
            ApplyCursorState(false);
        }

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            ApplyCursorState(true);
        }

        private void OnApplicationPause(bool paused)
        {
            isApplicationPaused = paused;
            ApplyCursorState(true);
        }

        private void ApplyCursorState(bool force)
        {
            bool shouldLock = hasFocus
                && !isApplicationPaused
                && PlayerInputGate.IsGameplay;
            CursorLockMode targetLockState = shouldLock
                ? CursorLockMode.Locked
                : CursorLockMode.None;
            bool targetVisibility = !shouldLock;

            if (force || Cursor.lockState != targetLockState)
            {
                Cursor.lockState = targetLockState;
            }

            if (force || Cursor.visible != targetVisibility)
            {
                Cursor.visible = targetVisibility;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
        }
    }
}

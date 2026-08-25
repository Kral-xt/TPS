using UnityEngine;

namespace TPS.Player.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EmojiBarController : MonoBehaviour
    {
        [Header("表情轮盘")]
        [SerializeField] private EmojiBar emojiBar;

        private IPlayerEmojiController playerEmojiController;
        private bool isEmojiBarVisible;
        private bool eventsRegistered;

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.T))
            {
                return;
            }

            if (isEmojiBarVisible)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void OnEnable()
        {
            PlayerInputGate.ModeChanged += OnInputModeChanged;
            RegisterEvents();
        }

        private void OnDisable()
        {
            PlayerInputGate.ModeChanged -= OnInputModeChanged;
            UnregisterEvents();
            CloseImmediate();
        }

        private void Open()
        {
            if (isEmojiBarVisible || emojiBar == null)
            {
                return;
            }

            isEmojiBarVisible = true;
            emojiBar.Show();
            PlayerInputGate.SetUI();
        }

        private void Close()
        {
            if (!isEmojiBarVisible)
            {
                return;
            }

            isEmojiBarVisible = false;
            emojiBar?.Hide();
            PlayerInputGate.SetGameplay();
        }

        private void CloseImmediate()
        {
            bool shouldRestoreGameplay = isEmojiBarVisible;
            isEmojiBarVisible = false;
            emojiBar?.HideImmediate();
            if (!shouldRestoreGameplay)
            {
                return;
            }

            PlayerInputGate.SetGameplay();
        }

        private void OnInputModeChanged(PlayerInputMode mode)
        {
            if (mode != PlayerInputMode.Gameplay || !isEmojiBarVisible)
            {
                return;
            }

            isEmojiBarVisible = false;
            emojiBar?.HideImmediate();
        }

        private void OnEmojiSelected(string trigger)
        {
            if (!string.IsNullOrWhiteSpace(trigger) && HasPlayerBinding())
            {
                playerEmojiController.PlayEmoji(trigger);
            }

            Close();
        }

        private bool HasPlayerBinding()
        {
            if (playerEmojiController == null)
            {
                return false;
            }

            if (playerEmojiController is Object unityObject && unityObject == null)
            {
                playerEmojiController = null;
                return false;
            }

            return true;
        }

        public void Bind(EmojiBar bar)
        {
            CloseImmediate();
            UnregisterEvents();
            emojiBar = bar;
            RegisterEvents();
            emojiBar?.HideImmediate();
            PlayerInputGate.SetGameplay();
        }

        public void BindPlayer(IPlayerEmojiController controller)
        {
            playerEmojiController = controller;
        }

        public void Unbind()
        {
            CloseImmediate();
            UnregisterEvents();
            emojiBar = null;
            playerEmojiController = null;
        }

        private void RegisterEvents()
        {
            if (eventsRegistered || emojiBar == null)
            {
                return;
            }

            emojiBar.EmojiSelected += OnEmojiSelected;
            eventsRegistered = true;
        }

        private void UnregisterEvents()
        {
            if (!eventsRegistered || emojiBar == null)
            {
                eventsRegistered = false;
                return;
            }

            emojiBar.EmojiSelected -= OnEmojiSelected;
            eventsRegistered = false;
        }
    }
}

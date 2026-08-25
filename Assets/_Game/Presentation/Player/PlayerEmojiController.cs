using System.Collections.Generic;
using TPS.Player.Application;
using TPS.Player.Domain;
using UnityEngine;

namespace TPS.Player.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PlayerEmojiController : MonoBehaviour, IPlayerEmojiController
    {
        private Animator animator;
        private PlayerAttributeController attributes;
        private readonly HashSet<int> emojiTriggerHashes = new HashSet<int>();
        private bool isPlayingEmoji;
        private int currentEmojiHash;

        private static readonly int StopEmojiHash = Animator.StringToHash("StopEmoji");

        public bool IsPlayingEmoji => isPlayingEmoji;

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
            attributes = GetComponent<PlayerAttributeController>();
            CacheEmojiTriggers();
        }

        private void OnEnable()
        {
            BindAttributeEvents();
        }

        private void OnDisable()
        {
            UnbindAttributeEvents();
            ClearState();
        }

        private void Update()
        {
            if (isPlayingEmoji && PlayerInputGate.IsGameplay && HasInterruptInput())
            {
                StopEmoji();
            }
        }

        public void PlayEmoji(string emojiName)
        {
            if (animator == null || string.IsNullOrWhiteSpace(emojiName))
            {
                return;
            }

            int emojiHash = Animator.StringToHash(emojiName);
            if (!emojiTriggerHashes.Contains(emojiHash))
            {
                Debug.LogWarning($"[PlayerEmojiController] Animator 缺少 Trigger：{emojiName}", this);
                return;
            }

            if (isPlayingEmoji && currentEmojiHash == emojiHash)
            {
                return;
            }

            ResetEmojiTriggers();
            animator.ResetTrigger(StopEmojiHash);
            currentEmojiHash = emojiHash;
            isPlayingEmoji = true;
            animator.SetTrigger(emojiHash);
        }

        public void StopEmoji()
        {
            if (!isPlayingEmoji)
            {
                return;
            }

            isPlayingEmoji = false;
            currentEmojiHash = 0;

            if (animator != null)
            {
                ResetEmojiTriggers();
                animator.SetTrigger(StopEmojiHash);
            }
        }

        public void ClearState()
        {
            isPlayingEmoji = false;
            currentEmojiHash = 0;

            if (animator != null)
            {
                ResetEmojiTriggers();
                animator.ResetTrigger(StopEmojiHash);
            }
        }

        private void CacheEmojiTriggers()
        {
            emojiTriggerHashes.Clear();
            if (animator == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger
                    && parameter.name.StartsWith("Emoji", System.StringComparison.Ordinal))
                {
                    emojiTriggerHashes.Add(parameter.nameHash);
                }
            }
        }

        private void ResetEmojiTriggers()
        {
            foreach (int triggerHash in emojiTriggerHashes)
            {
                animator.ResetTrigger(triggerHash);
            }
        }

        private static bool HasInterruptInput()
        {
            if (Input.GetAxisRaw("Horizontal") != 0f || Input.GetAxisRaw("Vertical") != 0f)
            {
                return true;
            }

            return Input.GetMouseButtonDown(0)
                || Input.GetMouseButtonDown(1)
                || Input.GetKeyDown(KeyCode.LeftAlt)
                || Input.GetKeyDown(KeyCode.Space);
        }

        private void BindAttributeEvents()
        {
            attributes ??= GetComponent<PlayerAttributeController>();
            if (attributes == null)
            {
                return;
            }

            attributes.Damaged -= OnPlayerDamaged;
            attributes.Died -= OnPlayerDied;
            attributes.Damaged += OnPlayerDamaged;
            attributes.Died += OnPlayerDied;
        }

        private void UnbindAttributeEvents()
        {
            if (attributes == null)
            {
                return;
            }

            attributes.Damaged -= OnPlayerDamaged;
            attributes.Died -= OnPlayerDied;
        }

        private void OnPlayerDamaged(PlayerDamagedEvent damageEvent)
        {
            StopEmoji();
        }

        private void OnPlayerDied(PlayerDiedEvent diedEvent)
        {
            StopEmoji();
        }
    }
}

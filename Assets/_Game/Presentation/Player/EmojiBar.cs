using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TPS.Player.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EmojiBar : MonoBehaviour
    {
        [Serializable]
        public class EmojiData
        {
            [Tooltip("表情按钮")]
            public Button button;

            [Tooltip("玩家 Animator 中对应的 Trigger；为空时隐藏按钮")]
            public string animatorTrigger;

            [NonSerialized] public UnityAction clickHandler;
        }

        [Header("表情按钮配置")]
        [SerializeField] private List<EmojiData> emojis = new List<EmojiData>();

        private Animator animator;
        private CanvasGroup canvasGroup;
        private bool isVisible;

        public event Action<string> EmojiSelected;

        public bool IsVisible => isVisible;

        private static readonly int ShowHash = Animator.StringToHash("Show");
        private static readonly int HideHash = Animator.StringToHash("Hide");
        private static readonly int HiddenStateHash = Animator.StringToHash("Hidden");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
            InitializeButtons();
            HideImmediate();
        }

        private void InitializeButtons()
        {
            foreach (EmojiData emoji in emojis)
            {
                if (emoji == null || emoji.button == null)
                {
                    continue;
                }

                string trigger = emoji.animatorTrigger?.Trim();
                if (string.IsNullOrEmpty(trigger))
                {
                    emoji.button.gameObject.SetActive(false);
                    continue;
                }

                emoji.animatorTrigger = trigger;
                emoji.button.gameObject.SetActive(true);
                emoji.clickHandler = () => OnEmojiClicked(trigger);
                emoji.button.onClick.AddListener(emoji.clickHandler);
            }
        }

        public void Show()
        {
            if (isVisible)
            {
                return;
            }

            isVisible = true;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                if (animator == null)
                {
                    canvasGroup.alpha = 1f;
                }
            }

            if (animator != null)
            {
                animator.ResetTrigger(HideHash);
                animator.SetTrigger(ShowHash);
            }
        }

        public void Hide()
        {
            if (!isVisible)
            {
                return;
            }

            isVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (animator != null)
            {
                animator.ResetTrigger(ShowHash);
                animator.SetTrigger(HideHash);
            }
            else if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        public void HideImmediate()
        {
            isVisible = false;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (animator != null)
            {
                animator.ResetTrigger(ShowHash);
                animator.ResetTrigger(HideHash);
                animator.Play(HiddenStateHash, 0, 0f);
            }
        }

        private void OnEmojiClicked(string trigger)
        {
            if (string.IsNullOrEmpty(trigger))
            {
                return;
            }

            EmojiSelected?.Invoke(trigger);
        }

        private void OnDestroy()
        {
            foreach (EmojiData emoji in emojis)
            {
                if (emoji != null && emoji.button != null && emoji.clickHandler != null)
                {
                    emoji.button.onClick.RemoveListener(emoji.clickHandler);
                }
            }
        }
    }
}

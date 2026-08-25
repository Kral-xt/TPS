
using TPS.Application.Abstractions;
using System.Collections;
using TPS.Combat.Application;
using TPS.Combat.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace TPS.UI.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(Animator))]
    public sealed class KillIconController : MonoBehaviour
    {
        private static readonly int ShowStateHash = Animator.StringToHash("KillIcon@Show");
        private static readonly int HideStateHash = Animator.StringToHash("KillIcon@Hide");

        [Header("击杀图标")]
        [SerializeField] private Sprite[] normalKillIcons = new Sprite[6];
        [SerializeField] private Sprite headShotIcon;

        [Header("显示")]
        [SerializeField, Min(0.01f)] private float displayDuration = 1f;

        private Image image;
        private Animator animator;
        private CanvasGroup canvasGroup;
        private Coroutine hideRoutine;
        private int killCount;
        private bool eventRegistered;

        public int KillCount => killCount;

        private void Awake()
        {
            image = GetComponent<Image>();
            animator = GetComponent<Animator>();
            canvasGroup = GetComponent<CanvasGroup>();
            HideImmediate();
        }

        private void OnEnable()
        {
            RegisterEvent();
        }

        private void OnDisable()
        {
            UnregisterEvent();
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            HideImmediate();
        }

        private void OnDestroy()
        {
            UnregisterEvent();
        }

        private void RegisterEvent()
        {
            if (eventRegistered)
            {
                return;
            }

            CombatEventHub.EnemyKilled += OnEnemyKilled;
            eventRegistered = true;
        }

        private void UnregisterEvent()
        {
            if (!eventRegistered)
            {
                return;
            }

            CombatEventHub.EnemyKilled -= OnEnemyKilled;
            eventRegistered = false;
        }

        private void OnEnemyKilled(EnemyKilledEvent killedEvent)
        {
            if (killedEvent.Source != DamageSourceKind.Player)
            {
                return;
            }

            killCount++;
            Sprite icon;
            if (killedEvent.IsHeadShot)
            {
                icon = headShotIcon;
            }
            else
            {
                icon = GetNormalKillIcon(killCount);
            }

            if (icon == null)
            {
                Debug.LogWarning("[KillIconController] 击杀图标未配置。", this);
            }
            else
            {
                Show(icon);
            }

            GameAudio.Current?.PlayKillFeedback(killCount, killedEvent.IsHeadShot);
        }


        private Sprite GetNormalKillIcon(int currentKillCount)
        {
            if (normalKillIcons == null || normalKillIcons.Length == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(currentKillCount - 1, 0, normalKillIcons.Length - 1);
            return normalKillIcons[index];
        }

        private void Show(Sprite icon)
        {
            image.sprite = icon;
            image.enabled = true;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (animator.runtimeAnimatorController != null)
            {
                animator.enabled = true;
                animator.Play(ShowStateHash, 0, 0f);
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(displayDuration);
            hideRoutine = null;

            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.Play(HideStateHash, 0, 0f);
            }
            else
            {
                HideImmediate();
            }
        }

        private void HideImmediate()
        {
            if (image != null)
            {
                image.enabled = false;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }
        }
    }
}

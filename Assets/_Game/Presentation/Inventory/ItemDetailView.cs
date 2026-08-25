using System;
using System.Collections;
using System.Text;
using TMPro;
using TPS.Application.Abstractions;
using TPS.Inventory.Application;
using UnityEngine;
using UnityEngine.UI;

namespace TPS.Inventory.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ItemDetailView : MonoBehaviour
    {
        private const string ShowStateName = "ItemDetail@Show";
        private const string HideStateName = "ItemDetail@hide";
        private static readonly int ShowStateHash =
            Animator.StringToHash("Base Layer." + ShowStateName);
        private static readonly int HideStateHash =
            Animator.StringToHash("Base Layer." + HideStateName);

        private RectTransform rectTransform;
        private Transform originalParent;
        private Vector2 originalAnchorMin;
        private Vector2 originalAnchorMax;
        private Vector2 originalPivot;
        private Vector3 originalAnchoredPosition;
        private Vector2 originalSizeDelta;
        private Vector3 originalLocalScale;
        private Image background;
        private Image icon;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI typeText;
        private TextMeshProUGUI descriptionText;
        private CanvasGroup canvasGroup;
        private Animator animator;
        private bool initialized;

        public RectTransform RectTransform => rectTransform;
        public bool IsVisible => initialized && gameObject.activeSelf;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            rectTransform = transform as RectTransform;
            originalParent = transform.parent;
            originalAnchorMin = rectTransform.anchorMin;
            originalAnchorMax = rectTransform.anchorMax;
            originalPivot = rectTransform.pivot;
            originalAnchoredPosition = rectTransform.anchoredPosition3D;
            originalSizeDelta = rectTransform.sizeDelta;
            originalLocalScale = rectTransform.localScale;

            background = transform.Find("Icon&Name/Bg")?.GetComponent<Image>();
            icon = transform.Find("Icon&Name/Icon")?.GetComponent<Image>();
            nameText = transform.Find("Icon&Name/Name")?.GetComponent<TextMeshProUGUI>();
            typeText = transform.Find("Icon&Name/Type")?.GetComponent<TextMeshProUGUI>();
            descriptionText = transform.Find("Description/Text")
                ?.GetComponent<TextMeshProUGUI>();
            canvasGroup = GetComponent<CanvasGroup>();
            animator = GetComponent<Animator>();
            initialized = true;

            if (background == null || icon == null || nameText == null
                || typeText == null || descriptionText == null
                || canvasGroup == null || animator == null)
            {
                Debug.LogError("[ItemDetailView] ItemDetail 结构或必要组件不完整。", this);
            }

            HideImmediate();
        }

        public void Show(InventoryDisplayItem item, RectTransform overlayRoot)
        {
            Initialize();
            if (item == null || overlayRoot == null)
            {
                return;
            }

            Refresh(item);
            gameObject.SetActive(true);
            rectTransform.SetParent(overlayRoot, true);
            rectTransform.localScale = Vector3.one;
            rectTransform.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            ClampToRoot(overlayRoot);

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            if (animator != null && animator.HasState(0, ShowStateHash))
            {
                animator.Play(ShowStateHash, 0, 0f);
                animator.Update(0f);
            }
            else
            {
                Debug.LogError(
                    $"[ItemDetailView] Animator 缺少状态：{ShowStateName}",
                    this);
            }
        }

        public IEnumerator HideAnimated()
        {
            Initialize();
            if (!gameObject.activeSelf)
            {
                yield break;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            if (animator == null || !animator.HasState(0, HideStateHash))
            {
                Debug.LogError(
                    $"[ItemDetailView] Animator 缺少状态：{HideStateName}",
                    this);
                HideImmediate();
                yield break;
            }

            animator.Play(HideStateHash, 0, 0f);
            animator.Update(0f);
            yield return null;

            float timeoutAt = Time.realtimeSinceStartup + 1f;
            while (Time.realtimeSinceStartup < timeoutAt)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.fullPathHash == HideStateHash
                    && stateInfo.normalizedTime >= 1f
                    && !animator.IsInTransition(0))
                {
                    break;
                }

                yield return null;
            }

            HideImmediate();
        }

        public void HideImmediate()
        {
            if (!initialized)
            {
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            RestoreOriginalLayout();
            gameObject.SetActive(false);
        }

        public bool ContainsScreenPoint(Vector2 screenPoint, Camera eventCamera)
        {
            return IsVisible
                && RectTransformUtility.RectangleContainsScreenPoint(
                    rectTransform,
                    screenPoint,
                    eventCamera);
        }

        private void Refresh(InventoryDisplayItem item)
        {
            if (background != null)
            {
                background.color = item.QualityColor;
            }

            if (icon != null)
            {
                icon.sprite = item.Icon;
                icon.enabled = item.Icon != null;
            }

            if (nameText != null)
            {
                nameText.text = item.Name;
            }

            if (typeText != null)
            {
                typeText.text = BuildTypeText(item.Categories);
            }

            if (descriptionText != null)
            {
                descriptionText.text = item.Description;
            }
        }

        private void RestoreOriginalLayout()
        {
            if (rectTransform == null || originalParent == null)
            {
                return;
            }

            rectTransform.SetParent(originalParent, false);
            rectTransform.anchorMin = originalAnchorMin;
            rectTransform.anchorMax = originalAnchorMax;
            rectTransform.pivot = originalPivot;
            rectTransform.anchoredPosition3D = originalAnchoredPosition;
            rectTransform.sizeDelta = originalSizeDelta;
            rectTransform.localScale = originalLocalScale;
        }

        private void ClampToRoot(RectTransform overlayRoot)
        {
            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(
                overlayRoot,
                rectTransform);
            Rect rootRect = overlayRoot.rect;
            Vector3 offset = Vector3.zero;

            if (bounds.min.x < rootRect.xMin)
            {
                offset.x += rootRect.xMin - bounds.min.x;
            }
            else if (bounds.max.x > rootRect.xMax)
            {
                offset.x -= bounds.max.x - rootRect.xMax;
            }

            if (bounds.min.y < rootRect.yMin)
            {
                offset.y += rootRect.yMin - bounds.min.y;
            }
            else if (bounds.max.y > rootRect.yMax)
            {
                offset.y -= bounds.max.y - rootRect.yMax;
            }

            rectTransform.localPosition += offset;
        }

        private static string BuildTypeText(InventoryItemCategory categories)
        {
            StringBuilder builder = new StringBuilder();
            AppendType(builder, categories, InventoryItemCategory.Equipment);
            AppendType(builder, categories, InventoryItemCategory.Material);
            AppendType(builder, categories, InventoryItemCategory.Fragment);
            AppendType(builder, categories, InventoryItemCategory.Other);
            AppendType(builder, categories, InventoryItemCategory.Usable);
            return builder.ToString();
        }

        private static void AppendType(
            StringBuilder builder,
            InventoryItemCategory categories,
            InventoryItemCategory category)
        {
            if ((categories & category) == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" / ");
            }

            builder.Append(category);
        }
    }
}

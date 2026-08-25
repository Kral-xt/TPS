using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using TMPro;
using TPS.Application.Abstractions;
using TPS.Inventory.Application;
using TPS.Inventory.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace QFramework.Example
{
	public class BagDialogData : UIPanelData
	{
	}
	public partial class BagDialog : UIPanel
	{
		private const string ShowStateName = "Bag@Show";
		private const string HideStateName = "Bag@Hide";
		private static readonly int ShowStateHash = Animator.StringToHash("Base Layer." + ShowStateName);
		private static readonly int HideStateHash = Animator.StringToHash("Base Layer." + HideStateName);

		[SerializeField, Min(1), Tooltip("每帧最多实例化的 ItemCell 数量")]
		private int itemCellsPerFrame = 20;
		[SerializeField, Min(0), Tooltip("可视区域上下额外保留的缓存行数")]
		private int itemCellBufferRows = 2;

		private readonly BagCategoryCache categoryCache = new BagCategoryCache();

		private BagDialogController controller;
		private RectTransform content;
		private ItemCellPool itemCellPool;
		private BagItemLoader itemLoader;
		private ItemDetailController itemDetailController;
		private ScrollRect scrollRect;
		private GridLayoutGroup gridLayout;
		private ContentSizeFitter contentSizeFitter;
		private TextMeshProUGUI bagSpaceNum;
		private Button quitButton;
		private Button allButton;
		private Button equipmentButton;
		private Button usableButton;
		private Button materialButton;
		private Button fragmentButton;
		private Button otherButton;
		private CanvasGroup canvasGroup;
		private Animator animator;
		private Coroutine animationRoutine;
		private InventoryItemCategory? selectedCategory;
		private bool eventsBound;
		private bool isClosing;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BagDialogData ?? new BagDialogData();
			ResolveReferences(false);
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as BagDialogData ?? mData;
		}

		public void Setup(
			BagDialogController owner,
			ItemCellPool cellPool,
			RectTransform prefabLayout)
		{
			controller = owner;
			itemCellPool = cellPool;
			isClosing = false;
			selectedCategory = null;
			ApplyPrefabLayout(prefabLayout);
			ResolveReferences(true);
			itemDetailController = GetComponent<ItemDetailController>();
			if (itemDetailController == null)
			{
				itemDetailController = gameObject.AddComponent<ItemDetailController>();
			}

			itemDetailController.Initialize(transform as RectTransform);
			itemLoader?.Dispose();
			itemLoader = new BagItemLoader(
				this,
				scrollRect,
				content,
				gridLayout,
				contentSizeFitter,
				itemCellPool,
				itemDetailController,
				itemCellsPerFrame,
				itemCellBufferRows);
			BindEvents();

			if (canvasGroup != null)
			{
				canvasGroup.interactable = true;
				canvasGroup.blocksRaycasts = true;
			}

			RefreshBag(true);
			UpdateNavigationSelection();
			PlayShowAnimation();
		}

		public void RefreshBag()
		{
			RefreshBag(false);
		}

		private void RefreshBag(bool resetScroll)
		{
			if (isClosing)
			{
				return;
			}

			ResolveReferences(true);
			itemDetailController?.CloseImmediate();

			PlayerInventoryController inventory = PlayerInventoryController.Instance;
			IReadOnlyList<InventoryDisplayItem> items = inventory != null
				? inventory.GetDisplayItems()
				: Array.Empty<InventoryDisplayItem>();

			categoryCache.Rebuild(items);
			if (bagSpaceNum != null)
			{
				bagSpaceNum.text = categoryCache.TotalCount.ToString();
			}

			itemLoader?.SetItems(
				categoryCache.GetItems(selectedCategory),
				resetScroll);
		}

		public void BeginClose(Action onCompleted)
		{
			if (isClosing)
			{
				return;
			}

			isClosing = true;
			itemDetailController?.CloseImmediate();
			itemLoader?.Suspend();
			if (canvasGroup != null)
			{
				canvasGroup.interactable = false;
				canvasGroup.blocksRaycasts = false;
			}

			StartAnimation(HideStateHash, HideStateName, onCompleted);
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			if (animationRoutine != null)
			{
				StopCoroutine(animationRoutine);
				animationRoutine = null;
			}

			UnbindEvents();
			itemDetailController?.CloseImmediate();
			itemLoader?.Dispose();
			itemLoader = null;
			itemCellPool = null;

			BagDialogController owner = controller;
			controller = null;
			owner?.NotifyDialogClosed(this);
		}

		private void ResolveReferences(bool validate)
		{
			content = content != null
				? content
				: transform.Find("MainBox/Scroll View/Viewport/Content") as RectTransform;
			scrollRect = scrollRect != null
				? scrollRect
				: transform.Find("MainBox/Scroll View")?.GetComponent<ScrollRect>();
			gridLayout = gridLayout != null
				? gridLayout
				: content?.GetComponent<GridLayoutGroup>();
			contentSizeFitter = contentSizeFitter != null
				? contentSizeFitter
				: content?.GetComponent<ContentSizeFitter>();
			bagSpaceNum = bagSpaceNum != null
				? bagSpaceNum
				: transform.Find("BagSpaceNum")?.GetComponent<TextMeshProUGUI>();
			quitButton = ResolveButton(quitButton, "QuitBtn");
			allButton = ResolveButton(allButton, "NavigationBar/AllBtn");
			equipmentButton = ResolveButton(equipmentButton, "NavigationBar/EquipmentBtn");
			usableButton = ResolveButton(usableButton, "NavigationBar/AvailableBtn");
			materialButton = ResolveButton(materialButton, "NavigationBar/MaterialBtn");
			fragmentButton = ResolveButton(fragmentButton, "NavigationBar/FragmentBtn");
			otherButton = ResolveButton(otherButton, "NavigationBar/OthersBtn");
			canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
			animator = animator != null ? animator : GetComponent<Animator>();

			if (validate && (content == null || itemCellPool == null || scrollRect == null
				|| gridLayout == null || bagSpaceNum == null
				|| quitButton == null || allButton == null || equipmentButton == null
				|| usableButton == null || materialButton == null
				|| fragmentButton == null || otherButton == null))
			{
				Debug.LogError("[BagDialog] Prefab 结构或必要组件不完整。", this);
			}
		}

		private void ApplyPrefabLayout(RectTransform source)
		{
			RectTransform target = transform as RectTransform;
			if (source == null || target == null)
			{
				Debug.LogError("[BagDialog] 无法恢复 Prefab 根 RectTransform 布局。", this);
				return;
			}

			target.anchorMin = source.anchorMin;
			target.anchorMax = source.anchorMax;
			target.pivot = source.pivot;
			target.anchoredPosition3D = source.anchoredPosition3D;
			target.sizeDelta = source.sizeDelta;
			target.localScale = source.localScale;
		}

		private Button ResolveButton(Button current, string path)
		{
			return current != null ? current : transform.Find(path)?.GetComponent<Button>();
		}

		private void BindEvents()
		{
			if (eventsBound)
			{
				return;
			}

			quitButton?.onClick.AddListener(OnQuitClicked);
			allButton?.onClick.AddListener(OnAllClicked);
			equipmentButton?.onClick.AddListener(OnEquipmentClicked);
			usableButton?.onClick.AddListener(OnUsableClicked);
			materialButton?.onClick.AddListener(OnMaterialClicked);
			fragmentButton?.onClick.AddListener(OnFragmentClicked);
			otherButton?.onClick.AddListener(OnOtherClicked);

			if (PlayerInventoryController.Instance != null)
			{
				PlayerInventoryController.Instance.InventoryChanged += RefreshBag;
			}

			eventsBound = true;
		}

		private void UnbindEvents()
		{
			if (!eventsBound)
			{
				return;
			}

			quitButton?.onClick.RemoveListener(OnQuitClicked);
			allButton?.onClick.RemoveListener(OnAllClicked);
			equipmentButton?.onClick.RemoveListener(OnEquipmentClicked);
			usableButton?.onClick.RemoveListener(OnUsableClicked);
			materialButton?.onClick.RemoveListener(OnMaterialClicked);
			fragmentButton?.onClick.RemoveListener(OnFragmentClicked);
			otherButton?.onClick.RemoveListener(OnOtherClicked);

			if (PlayerInventoryController.Instance != null)
			{
				PlayerInventoryController.Instance.InventoryChanged -= RefreshBag;
			}

			eventsBound = false;
		}

		private void SetFilter(InventoryItemCategory? category)
		{
			if (selectedCategory == category)
			{
				return;
			}

			selectedCategory = category;
			itemDetailController?.CloseImmediate();
			itemLoader?.SetItems(categoryCache.GetItems(selectedCategory), true);
			UpdateNavigationSelection();
		}

		private void UpdateNavigationSelection()
		{
			SetButtonSelected(allButton, !selectedCategory.HasValue);
			SetButtonSelected(
				equipmentButton,
				selectedCategory == InventoryItemCategory.Equipment);
			SetButtonSelected(usableButton, selectedCategory == InventoryItemCategory.Usable);
			SetButtonSelected(
				materialButton,
				selectedCategory == InventoryItemCategory.Material);
			SetButtonSelected(
				fragmentButton,
				selectedCategory == InventoryItemCategory.Fragment);
			SetButtonSelected(otherButton, selectedCategory == InventoryItemCategory.Other);
		}

		private void SetButtonSelected(Button button, bool selected)
		{
			Transform selectedView = button != null ? button.transform.Find("Selected") : null;
			if (selectedView != null)
			{
				selectedView.gameObject.SetActive(selected);
			}
		}

		private void PlayShowAnimation()
		{
			StartAnimation(ShowStateHash, ShowStateName, null);
		}

		private void StartAnimation(int stateHash, string stateName, Action onCompleted)
		{
			if (animationRoutine != null)
			{
				StopCoroutine(animationRoutine);
			}

			animationRoutine = StartCoroutine(
				PlayAnimationAndWait(stateHash, stateName, onCompleted));
		}

		private IEnumerator PlayAnimationAndWait(
			int stateHash,
			string stateName,
			Action onCompleted)
		{
			if (animator == null || !animator.HasState(0, stateHash))
			{
				Debug.LogError($"[BagDialog] Animator 缺少状态：{stateName}", this);
				animationRoutine = null;
				onCompleted?.Invoke();
				yield break;
			}

			animator.Play(stateHash, 0, 0f);
			animator.Update(0f);
			yield return null;

			float timeoutAt = Time.realtimeSinceStartup + 5f;
			while (Time.realtimeSinceStartup < timeoutAt)
			{
				AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
				if (stateInfo.fullPathHash == stateHash
					&& stateInfo.normalizedTime >= 1f
					&& !animator.IsInTransition(0))
				{
					break;
				}

				yield return null;
			}

			if (Time.realtimeSinceStartup >= timeoutAt)
			{
				Debug.LogWarning($"[BagDialog] 等待动画超时：{stateName}", this);
			}

			animationRoutine = null;
			onCompleted?.Invoke();
		}

		private void OnQuitClicked()
		{
			controller?.CloseBag();
		}

		private void OnAllClicked() => SetFilter(null);
		private void OnEquipmentClicked() => SetFilter(InventoryItemCategory.Equipment);
		private void OnUsableClicked() => SetFilter(InventoryItemCategory.Usable);
		private void OnMaterialClicked() => SetFilter(InventoryItemCategory.Material);
		private void OnFragmentClicked() => SetFilter(InventoryItemCategory.Fragment);
		private void OnOtherClicked() => SetFilter(InventoryItemCategory.Other);
	}
}

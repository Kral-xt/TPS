using TMPro;
using TPS.Application.Abstractions;
using TPS.Inventory.Application;
using UnityEngine;
using UnityEngine.UI;

namespace TPS.Inventory.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ItemCellController : MonoBehaviour
    {
        private Image background;
        private Image itemIcon;
        private Transform countRoot;
        private TextMeshProUGUI countText;
        private Button detailButton;
        private ItemDetailView detailView;
        private ItemDetailController detailController;
        private InventoryDisplayItem displayItem;
        private int boundDataIndex = -1;
        private bool detailClickBound;

        public int ItemID => displayItem?.ItemID ?? 0;

        public void Bind(InventoryDisplayItem item, bool showCount = true)
        {
            Refresh(item, showCount);
        }

        public void Refresh(InventoryDisplayItem item, bool showCount = true)
        {
            if (displayItem != null && !ReferenceEquals(displayItem, item))
            {
                detailController?.NotifyCellRecycled(this);
            }

            boundDataIndex = -1;
            ApplyVisuals(item, showCount);
        }

        public void BindForBag(
            InventoryDisplayItem item,
            int dataIndex,
            ItemDetailController owner,
            bool showCount = false)
        {
            if (boundDataIndex != dataIndex
                || !ReferenceEquals(displayItem, item)
                || detailController != owner)
            {
                detailController?.NotifyCellRecycled(this);
            }

            detailController = owner;
            boundDataIndex = dataIndex;
            ApplyVisuals(item, showCount);
        }

        public bool IsBoundTo(InventoryDisplayItem item)
        {
            return gameObject.activeInHierarchy
                && ReferenceEquals(displayItem, item);
        }

        private void ApplyVisuals(InventoryDisplayItem item, bool showCount)
        {
            displayItem = item;
            ResolveReferences();

            if (background != null)
            {
                background.color = item.QualityColor;
            }

            if (itemIcon != null)
            {
                itemIcon.sprite = item.Icon;
                itemIcon.enabled = item.Icon != null;
            }

            if (countText != null)
            {
                countText.text = $"x{item.Count}";
            }

            if (countRoot != null)
            {
                countRoot.gameObject.SetActive(showCount);
            }
        }

        public void Clear()
        {
            detailController?.NotifyCellRecycled(this);
            detailView?.HideImmediate();
            detailController = null;
            displayItem = null;
            boundDataIndex = -1;
            ResolveReferences();

            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (countRoot != null)
            {
                countRoot.gameObject.SetActive(false);
            }
        }

        public bool HasCategory(InventoryItemCategory category)
        {
            return displayItem != null && displayItem.HasCategory(category);
        }

        private void ResolveReferences()
        {
            if (background == null)
            {
                background = transform.Find("Bg")?.GetComponent<Image>();
            }

            if (itemIcon == null)
            {
                itemIcon = transform.Find("ItemIcon")?.GetComponent<Image>();
            }

            if (countRoot == null)
            {
                countRoot = transform.Find("ItemCount");
            }

            if (countText == null)
            {
                countText = countRoot?.Find("Count")?.GetComponent<TextMeshProUGUI>();
            }

            if (detailButton == null)
            {
                detailButton = transform.Find("Btn")?.GetComponent<Button>();
            }

            if (detailView == null)
            {
                Transform detailRoot = transform.Find("ItemDetail");
                if (detailRoot != null)
                {
                    detailView = detailRoot.GetComponent<ItemDetailView>();
                    if (detailView == null)
                    {
                        detailView = detailRoot.gameObject.AddComponent<ItemDetailView>();
                    }

                    detailView.Initialize();
                }
            }

            if (!detailClickBound && detailButton != null)
            {
                detailButton.onClick.AddListener(OnDetailClicked);
                detailClickBound = true;
            }

            if (background == null || itemIcon == null || countText == null
                || detailButton == null || detailView == null)
            {
                Debug.LogError(
                    "[ItemCellController] ItemCell 结构不完整，需要基础显示节点、Btn 和 ItemDetail。",
                    this);
            }
        }

        private void OnDetailClicked()
        {
            if (displayItem != null && detailController != null && detailView != null)
            {
                detailController.OpenDetail(this, detailView, displayItem);
            }
        }

        private void OnDestroy()
        {
            detailController?.NotifyCellRecycled(this);
            if (detailClickBound && detailButton != null)
            {
                detailButton.onClick.RemoveListener(OnDetailClicked);
            }
        }
    }
}

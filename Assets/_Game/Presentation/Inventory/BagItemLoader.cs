using System;
using System.Collections;
using System.Collections.Generic;
using TPS.Inventory.Application;
using UnityEngine;
using UnityEngine.UI;

namespace TPS.Inventory.Presentation
{
    public sealed class BagItemLoader : IDisposable
    {
        private static readonly IReadOnlyList<InventoryDisplayItem> EmptyItems =
            Array.Empty<InventoryDisplayItem>();

        private readonly MonoBehaviour coroutineHost;
        private readonly ScrollRect scrollRect;
        private readonly RectTransform content;
        private readonly RectTransform viewport;
        private readonly GridLayoutGroup gridLayout;
        private readonly ContentSizeFitter contentSizeFitter;
        private readonly ItemCellPool cellPool;
        private readonly ItemDetailController itemDetailController;
        private readonly int maxCreatesPerFrame;
        private readonly int bufferRows;

        private IReadOnlyList<InventoryDisplayItem> items = EmptyItems;
        private Coroutine loadRoutine;
        private Vector2 cellSize;
        private Vector2 spacing;
        private int columns;
        private int paddingLeft;
        private int paddingTop;
        private int paddingBottom;
        private int firstDataIndex = -1;
        private int requiredCellCount;
        private int desiredPoolCount;
        private bool disposed;
        private bool suspended;

        public BagItemLoader(
            MonoBehaviour host,
            ScrollRect ownerScrollRect,
            RectTransform ownerContent,
            GridLayoutGroup ownerGridLayout,
            ContentSizeFitter ownerContentSizeFitter,
            ItemCellPool pool,
            ItemDetailController detailController,
            int createsPerFrame,
            int cachedRows)
        {
            coroutineHost = host != null
                ? host
                : throw new ArgumentNullException(nameof(host));
            scrollRect = ownerScrollRect != null
                ? ownerScrollRect
                : throw new ArgumentNullException(nameof(ownerScrollRect));
            content = ownerContent != null
                ? ownerContent
                : throw new ArgumentNullException(nameof(ownerContent));
            viewport = scrollRect.viewport != null
                ? scrollRect.viewport
                : throw new ArgumentNullException(nameof(scrollRect.viewport));
            gridLayout = ownerGridLayout != null
                ? ownerGridLayout
                : throw new ArgumentNullException(nameof(ownerGridLayout));
            contentSizeFitter = ownerContentSizeFitter;
            cellPool = pool ?? throw new ArgumentNullException(nameof(pool));
            itemDetailController = detailController
                ?? throw new ArgumentNullException(nameof(detailController));
            maxCreatesPerFrame = Mathf.Max(1, createsPerFrame);
            bufferRows = Mathf.Max(0, cachedRows);

            CaptureLayout();
            gridLayout.enabled = false;
            if (contentSizeFitter != null)
            {
                contentSizeFitter.enabled = false;
            }

            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        }

        public int TotalItemCount => items.Count;
        public int FirstDataIndex => firstDataIndex;
        public int ActiveCellCount => cellPool.ActiveCount;

        public void SetItems(
            IReadOnlyList<InventoryDisplayItem> source,
            bool resetScroll)
        {
            if (disposed)
            {
                return;
            }

            suspended = false;
            StopLoading();
            items = source ?? EmptyItems;
            Canvas.ForceUpdateCanvases();
            UpdateContentHeight();

            if (resetScroll)
            {
                scrollRect.StopMovement();
                content.anchoredPosition = new Vector2(
                    content.anchoredPosition.x,
                    0f);
            }

            RefreshVisibleRange(true);
        }

        public void Suspend()
        {
            suspended = true;
            StopLoading();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            StopLoading();
            scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
            cellPool.ReleaseAll();
            gridLayout.enabled = true;
            if (contentSizeFitter != null)
            {
                contentSizeFitter.enabled = true;
            }
        }

        private void CaptureLayout()
        {
            cellSize = gridLayout.cellSize;
            spacing = gridLayout.spacing;
            paddingLeft = gridLayout.padding.left;
            paddingTop = gridLayout.padding.top;
            paddingBottom = gridLayout.padding.bottom;

            if (gridLayout.startAxis != GridLayoutGroup.Axis.Horizontal
                || gridLayout.startCorner != GridLayoutGroup.Corner.UpperLeft)
            {
                Debug.LogError(
                    "[BagItemLoader] 当前虚拟列表仅支持从左上开始的横向 GridLayoutGroup。",
                    coroutineHost);
            }

            if (gridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                columns = Mathf.Max(1, gridLayout.constraintCount);
                return;
            }

            float availableWidth = Mathf.Max(
                cellSize.x,
                viewport.rect.width - gridLayout.padding.horizontal);
            columns = Mathf.Max(
                1,
                Mathf.FloorToInt(
                    (availableWidth + spacing.x)
                    / Mathf.Max(1f, cellSize.x + spacing.x)));
        }

        private void UpdateContentHeight()
        {
            int rows = Mathf.CeilToInt(items.Count / (float)columns);
            float contentHeight = paddingTop
                + paddingBottom
                + rows * cellSize.y
                + Mathf.Max(0, rows - 1) * spacing.y;
            contentHeight = Mathf.Max(viewport.rect.height, contentHeight);
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                contentHeight);
        }

        private void OnScrollValueChanged(Vector2 _)
        {
            RefreshVisibleRange(false);
        }

        private void RefreshVisibleRange(bool forceRebind)
        {
            if (disposed || suspended)
            {
                return;
            }

            CalculateVisibleRange(out int nextFirstIndex, out int nextCellCount);
            if (!forceRebind
                && nextFirstIndex == firstDataIndex
                && nextCellCount == requiredCellCount
                && cellPool.ActiveCount == requiredCellCount)
            {
                return;
            }

            StopLoading();
            firstDataIndex = nextFirstIndex;
            requiredCellCount = nextCellCount;
            desiredPoolCount = CalculateDesiredPoolCount();
            cellPool.ReleaseExcess(requiredCellCount);
            BindActiveCells();

            if (cellPool.ActiveCount < requiredCellCount
                || cellPool.TotalCount < desiredPoolCount)
            {
                loadRoutine = coroutineHost.StartCoroutine(LoadVisibleCells());
            }
        }

        private void CalculateVisibleRange(
            out int startIndex,
            out int cellCount)
        {
            if (items.Count == 0)
            {
                startIndex = 0;
                cellCount = 0;
                return;
            }

            float rowStep = Mathf.Max(1f, cellSize.y + spacing.y);
            int totalRows = Mathf.CeilToInt(items.Count / (float)columns);
            float scrollOffset = Mathf.Max(0f, content.anchoredPosition.y);
            int firstVisibleRow = Mathf.Clamp(
                Mathf.FloorToInt(
                    Mathf.Max(0f, scrollOffset - paddingTop) / rowStep),
                0,
                totalRows - 1);
            int visibleRows = Mathf.Max(
                1,
                Mathf.CeilToInt(viewport.rect.height / rowStep));
            int startRow = Mathf.Max(0, firstVisibleRow - bufferRows);
            int endRowExclusive = Mathf.Min(
                totalRows,
                firstVisibleRow + visibleRows + bufferRows);

            startIndex = startRow * columns;
            int endIndex = Mathf.Min(items.Count, endRowExclusive * columns);
            cellCount = Mathf.Max(0, endIndex - startIndex);
        }

        private int CalculateDesiredPoolCount()
        {
            float rowStep = Mathf.Max(1f, cellSize.y + spacing.y);
            int visibleRows = Mathf.Max(
                1,
                Mathf.CeilToInt(viewport.rect.height / rowStep));
            int desiredRows = visibleRows + bufferRows * 2 + 1;
            return Mathf.Min(items.Count, desiredRows * columns);
        }

        private IEnumerator LoadVisibleCells()
        {
            while (!disposed
                && !suspended
                && (cellPool.ActiveCount < requiredCellCount
                    || cellPool.TotalCount < desiredPoolCount))
            {
                int createdThisFrame = 0;
                while (cellPool.ActiveCount < requiredCellCount)
                {
                    cellPool.Acquire(content, out bool instantiated);
                    if (instantiated)
                    {
                        createdThisFrame++;
                        if (createdThisFrame >= maxCreatesPerFrame)
                        {
                            break;
                        }
                    }
                }

                while (cellPool.TotalCount < desiredPoolCount
                    && createdThisFrame < maxCreatesPerFrame)
                {
                    cellPool.PrewarmOne();
                    createdThisFrame++;
                }

                BindActiveCells();
                if (cellPool.ActiveCount < requiredCellCount
                    || cellPool.TotalCount < desiredPoolCount)
                {
                    yield return null;
                }
            }

            loadRoutine = null;
        }

        private void BindActiveCells()
        {
            for (int slotIndex = 0; slotIndex < cellPool.ActiveCount; slotIndex++)
            {
                int dataIndex = firstDataIndex + slotIndex;
                ItemCellController cell = cellPool.GetActiveCell(slotIndex);
                if (cell == null || dataIndex < 0 || dataIndex >= items.Count)
                {
                    continue;
                }

                InventoryDisplayItem item = items[dataIndex];
                cell.BindForBag(
                    item,
                    dataIndex,
                    itemDetailController,
                    false);
                cell.gameObject.name = $"ItemCell_{item.ItemID}_{dataIndex}";
                PositionCell(cell.transform as RectTransform, dataIndex);
            }
        }

        private void PositionCell(RectTransform cell, int dataIndex)
        {
            if (cell == null)
            {
                return;
            }

            int row = dataIndex / columns;
            int column = dataIndex % columns;
            cell.anchorMin = new Vector2(0f, 1f);
            cell.anchorMax = new Vector2(0f, 1f);
            cell.pivot = new Vector2(0.5f, 0.5f);
            cell.sizeDelta = cellSize;
            cell.anchoredPosition = new Vector2(
                paddingLeft + cellSize.x * 0.5f + column * (cellSize.x + spacing.x),
                -(paddingTop + cellSize.y * 0.5f + row * (cellSize.y + spacing.y)));
        }

        private void StopLoading()
        {
            if (loadRoutine == null)
            {
                return;
            }

            coroutineHost.StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
    }
}

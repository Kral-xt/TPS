using System.Collections;
using TPS.Inventory.Application;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ItemDetailController : MonoBehaviour
    {
        private RectTransform overlayRoot;
        private Canvas rootCanvas;
        private ItemCellController activeCell;
        private ItemDetailView activeView;
        private ItemCellController pendingCell;
        private ItemDetailView pendingView;
        private InventoryDisplayItem pendingItem;
        private Coroutine transitionRoutine;
        private int lastOpenFrame = -1;

        public bool IsOpen => activeView != null && activeView.IsVisible;

        public void Initialize(RectTransform ownerRoot)
        {
            overlayRoot = ownerRoot;
            rootCanvas = ownerRoot != null ? ownerRoot.GetComponentInParent<Canvas>() : null;
        }

        public void OpenDetail(
            ItemCellController cell,
            ItemDetailView view,
            InventoryDisplayItem item)
        {
            if (overlayRoot == null || cell == null || view == null || item == null)
            {
                return;
            }

            lastOpenFrame = Time.frameCount;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                activeView?.HideImmediate();
                ClearActive();
            }

            if (activeCell == cell && activeView == view)
            {
                activeView.Show(item, overlayRoot);
                return;
            }

            pendingCell = cell;
            pendingView = view;
            pendingItem = item;
            if (activeView == null)
            {
                ShowPending();
                return;
            }

            transitionRoutine = StartCoroutine(SwitchDetail());
        }

        public void CloseDetail()
        {
            pendingCell = null;
            pendingView = null;
            pendingItem = null;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
                activeView?.HideImmediate();
                ClearActive();
                return;
            }

            if (activeView != null)
            {
                transitionRoutine = StartCoroutine(CloseActive());
            }
        }

        public void CloseImmediate()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            pendingCell = null;
            pendingView = null;
            pendingItem = null;
            activeView?.HideImmediate();
            ClearActive();
        }

        public void NotifyCellRecycled(ItemCellController cell)
        {
            if (cell != null && (cell == activeCell || cell == pendingCell))
            {
                CloseImmediate();
            }
        }

        public void HandlePointerDown(Vector2 screenPoint)
        {
            if (!IsOpen)
            {
                return;
            }

            Camera eventCamera = rootCanvas != null
                && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? rootCanvas.worldCamera
                    : null;
            if (!activeView.ContainsScreenPoint(screenPoint, eventCamera))
            {
                CloseDetail();
            }
        }

        private void LateUpdate()
        {
            if (!IsOpen
                || Time.frameCount == lastOpenFrame
                || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            HandlePointerDown(Input.mousePosition);
        }

        private IEnumerator SwitchDetail()
        {
            ItemDetailView previousView = activeView;
            yield return previousView.HideAnimated();
            if (activeView == previousView)
            {
                ClearActive();
            }

            ShowPending();
            transitionRoutine = null;
        }

        private IEnumerator CloseActive()
        {
            ItemDetailView closingView = activeView;
            yield return closingView.HideAnimated();
            if (activeView == closingView)
            {
                ClearActive();
            }

            transitionRoutine = null;
        }

        private void ShowPending()
        {
            if (pendingCell == null
                || pendingView == null
                || pendingItem == null
                || !pendingCell.IsBoundTo(pendingItem))
            {
                pendingCell = null;
                pendingView = null;
                pendingItem = null;
                return;
            }

            activeCell = pendingCell;
            activeView = pendingView;
            InventoryDisplayItem item = pendingItem;
            pendingCell = null;
            pendingView = null;
            pendingItem = null;
            activeView.Show(item, overlayRoot);
        }

        private void ClearActive()
        {
            activeCell = null;
            activeView = null;
        }

        private void OnDisable()
        {
            CloseImmediate();
        }

        private void OnDestroy()
        {
            CloseImmediate();
        }
    }
}

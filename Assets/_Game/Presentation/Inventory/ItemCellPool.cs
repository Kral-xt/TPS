using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.Inventory.Presentation
{
    public sealed class ItemCellPool : IDisposable
    {
        private readonly GameObject itemCellPrefab;
        private readonly RectTransform cacheRoot;
        private readonly List<ItemCellController> activeCells =
            new List<ItemCellController>();
        private readonly Stack<ItemCellController> cachedCells =
            new Stack<ItemCellController>();

        private bool disposed;
        private int totalCreated;

        public ItemCellPool(GameObject prefab, Transform owner)
        {
            itemCellPrefab = prefab != null
                ? prefab
                : throw new ArgumentNullException(nameof(prefab));
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            GameObject cacheObject = new GameObject(
                "BagItemCellCache",
                typeof(RectTransform));
            cacheRoot = cacheObject.GetComponent<RectTransform>();
            cacheRoot.SetParent(owner, false);
            cacheObject.SetActive(false);
        }

        public int ActiveCount => activeCells.Count;
        public int CachedCount => cachedCells.Count;
        public int TotalCount => activeCells.Count + cachedCells.Count;
        public int TotalCreated => totalCreated;

        public ItemCellController GetActiveCell(int index)
        {
            return index >= 0 && index < activeCells.Count
                ? activeCells[index]
                : null;
        }

        public ItemCellController Acquire(
            RectTransform parent,
            out bool instantiated)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ItemCellPool));
            }

            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            ItemCellController cell = TakeCachedCell();
            instantiated = cell == null;
            if (instantiated)
            {
                GameObject cellObject = UnityEngine.Object.Instantiate(
                    itemCellPrefab,
                    parent,
                    false);
                cell = cellObject.GetComponent<ItemCellController>();
                if (cell == null)
                {
                    cell = cellObject.AddComponent<ItemCellController>();
                }

                totalCreated++;
            }
            else
            {
                cell.transform.SetParent(parent, false);
            }

            cell.gameObject.SetActive(true);
            activeCells.Add(cell);
            return cell;
        }

        public void PrewarmOne()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ItemCellPool));
            }

            GameObject cellObject = UnityEngine.Object.Instantiate(
                itemCellPrefab,
                cacheRoot,
                false);
            ItemCellController cell = cellObject.GetComponent<ItemCellController>();
            if (cell == null)
            {
                cell = cellObject.AddComponent<ItemCellController>();
            }

            cell.Clear();
            cellObject.SetActive(false);
            cachedCells.Push(cell);
            totalCreated++;
        }

        public void ReleaseExcess(int activeCount)
        {
            int targetCount = Mathf.Max(0, activeCount);
            while (activeCells.Count > targetCount)
            {
                int lastIndex = activeCells.Count - 1;
                ItemCellController cell = activeCells[lastIndex];
                activeCells.RemoveAt(lastIndex);
                Cache(cell);
            }
        }

        public void ReleaseAll()
        {
            ReleaseExcess(0);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            ReleaseAll();
            cachedCells.Clear();
            if (cacheRoot != null)
            {
                UnityEngine.Object.Destroy(cacheRoot.gameObject);
            }
        }

        private ItemCellController TakeCachedCell()
        {
            while (cachedCells.Count > 0)
            {
                ItemCellController cell = cachedCells.Pop();
                if (cell != null)
                {
                    return cell;
                }
            }

            return null;
        }

        private void Cache(ItemCellController cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.Clear();
            cell.gameObject.SetActive(false);
            cell.transform.SetParent(cacheRoot, false);
            cachedCells.Push(cell);
        }
    }
}

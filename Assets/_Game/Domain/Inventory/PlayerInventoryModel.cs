using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TPS.Inventory.Domain
{
    public sealed class PlayerInventoryModel
    {
        private readonly List<ItemData> items = new List<ItemData>();
        private readonly Dictionary<int, int> itemTotals = new Dictionary<int, int>();
        private readonly ReadOnlyDictionary<int, int> readOnlyItemTotals;

        public PlayerInventoryModel()
        {
            readOnlyItemTotals = new ReadOnlyDictionary<int, int>(itemTotals);
        }

        public IReadOnlyDictionary<int, int> ItemTotals => readOnlyItemTotals;
        public int EntryCount => items.Count;

        public void AddItem(int itemID, int count)
        {
            if (itemID <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemID), "物品 ID 必须大于 0。");
            }

            if (count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "添加数量必须大于 0。");
            }

            long nextCount = GetTotalCount(itemID) + count;
            if (nextCount > int.MaxValue)
            {
                throw new OverflowException($"物品 {itemID} 的数量超过 Int32 上限。");
            }

            items.Add(new ItemData(itemID, count));
            itemTotals[itemID] = (int)nextCount;
        }

        public bool RemoveItem(int itemID, int count)
        {
            if (itemID <= 0 || count <= 0 || GetTotalCount(itemID) < count)
            {
                return false;
            }

            int remainingToRemove = count;
            for (int index = 0; index < items.Count && remainingToRemove > 0;)
            {
                ItemData item = items[index];
                if (item.ItemID != itemID)
                {
                    index++;
                    continue;
                }

                if (item.Count <= remainingToRemove)
                {
                    remainingToRemove -= item.Count;
                    items.RemoveAt(index);
                    continue;
                }

                item.Count -= remainingToRemove;
                remainingToRemove = 0;
            }

            int remainingCount = GetItemCount(itemID) - count;
            if (remainingCount <= 0)
            {
                itemTotals.Remove(itemID);
            }
            else
            {
                itemTotals[itemID] = remainingCount;
            }

            return true;
        }

        public int GetItemCount(int itemID)
        {
            return itemTotals.TryGetValue(itemID, out int count) ? count : 0;
        }

        public bool HasItem(int itemID)
        {
            return GetItemCount(itemID) > 0;
        }

        public void Restore(InventoryData data)
        {
            items.Clear();
            itemTotals.Clear();
            if (data?.Items == null)
            {
                return;
            }

            foreach (ItemData itemData in data.Items)
            {
                if (itemData == null || itemData.ItemID <= 0 || itemData.Count <= 0)
                {
                    continue;
                }

                long restoredCount = GetTotalCount(itemData.ItemID) + itemData.Count;
                if (restoredCount > int.MaxValue)
                {
                    continue;
                }

                items.Add(new ItemData(itemData.ItemID, itemData.Count));
                itemTotals[itemData.ItemID] = (int)restoredCount;
            }
        }

        public InventoryData Capture()
        {
            InventoryData data = new InventoryData();
            for (int index = 0; index < items.Count; index++)
            {
                ItemData item = items[index];
                data.Items.Add(new ItemData(item.ItemID, item.Count));
            }

            return data;
        }

        private long GetTotalCount(int itemID)
        {
            return itemTotals.TryGetValue(itemID, out int count) ? count : 0L;
        }
    }
}

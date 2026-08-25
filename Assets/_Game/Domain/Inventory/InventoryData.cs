using System;
using System.Collections.Generic;

namespace TPS.Inventory.Domain
{
    [Serializable]
    public sealed class ItemData
    {
        public int ItemID;
        public int Count;

        public ItemData()
        {
        }

        public ItemData(int itemID, int count)
        {
            ItemID = itemID;
            Count = count;
        }
    }

    [Serializable]
    public sealed class InventoryData
    {
        public List<ItemData> Items = new List<ItemData>();
    }
}

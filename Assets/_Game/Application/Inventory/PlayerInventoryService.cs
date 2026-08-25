using System;
using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.Inventory.Domain;

namespace TPS.Inventory.Application
{
    public sealed class PlayerInventoryService
    {
        private readonly IInventoryStore store;
        private readonly PlayerInventoryModel model = new PlayerInventoryModel();
        private bool isInitialized;

        public PlayerInventoryService(IInventoryStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IReadOnlyDictionary<int, int> Items => model.ItemTotals;
        public IReadOnlyList<ItemData> ItemEntries => model.Capture().Items.AsReadOnly();
        public int EntryCount => model.EntryCount;

        public void Initialize()
        {
            if (isInitialized)
            {
                return;
            }

            if (store.TryLoad(out InventoryData data))
            {
                model.Restore(data);
            }

            isInitialized = true;
        }

        public void AddItem(int itemID, int count)
        {
            EnsureInitialized();
            model.AddItem(itemID, count);
            SaveInventory();
        }

        public bool RemoveItem(int itemID, int count)
        {
            EnsureInitialized();
            if (!model.RemoveItem(itemID, count))
            {
                return false;
            }

            SaveInventory();
            return true;
        }

        public int GetItemCount(int itemID)
        {
            EnsureInitialized();
            return model.GetItemCount(itemID);
        }

        public bool HasItem(int itemID)
        {
            EnsureInitialized();
            return model.HasItem(itemID);
        }

        public bool SaveInventory()
        {
            EnsureInitialized();
            return store.Save(model.Capture());
        }

        private void EnsureInitialized()
        {
            if (!isInitialized)
            {
                Initialize();
            }
        }
    }
}

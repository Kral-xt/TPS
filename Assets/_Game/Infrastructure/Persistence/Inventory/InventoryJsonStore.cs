using System;
using TPS.Application.Abstractions;
using TPS.Inventory.Domain;
using UnityEngine;

namespace TPS.Inventory.Infrastructure
{
    public sealed class InventoryJsonStore : IInventoryStore
    {
        private const string DefaultSlot = "inventory";

        private readonly ISaveRepository repository;
        private readonly string slot;

        public InventoryJsonStore(ISaveRepository repository, string slot = DefaultSlot)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.slot = string.IsNullOrWhiteSpace(slot) ? DefaultSlot : slot;
        }

        public bool TryLoad(out InventoryData data)
        {
            data = new InventoryData();

            try
            {
                if (!repository.Exists(slot))
                {
                    return true;
                }

                string json = repository.Load(slot);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return true;
                }

                InventoryData loadedData = JsonUtility.FromJson<InventoryData>(json);
                if (loadedData == null)
                {
                    Debug.LogError("[InventoryJsonStore] inventory.json 无法反序列化，已使用空背包。");
                    return false;
                }

                loadedData.Items ??= new System.Collections.Generic.List<ItemData>();
                data = loadedData;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryJsonStore] 读取背包存档失败，已使用空背包。\n{exception}");
                return false;
            }
        }

        public bool Save(InventoryData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data ?? new InventoryData(), true);
                repository.Save(slot, json);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[InventoryJsonStore] 保存背包存档失败。\n{exception}");
                return false;
            }
        }
    }
}

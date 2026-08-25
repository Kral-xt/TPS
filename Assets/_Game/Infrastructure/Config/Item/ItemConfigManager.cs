using System.Collections.Generic;
using UnityEngine;

namespace TPS.ItemSystem.Infrastructure
{
    public sealed class ItemConfigManager
    {
        private const string ItemResourcePath = "Item";
        private const string RuntimeConfigResourcePath = "Config/Item/ItemRuntimeConfig";

        private static ItemConfigManager instance;

        private readonly Dictionary<int, ItemConfig> itemById = new Dictionary<int, ItemConfig>();
        private readonly HashSet<int> duplicateItemIds = new HashSet<int>();
        private ItemQualityConfig qualityConfig;

        private ItemConfigManager()
        {
            LoadConfigs();
        }

        public static ItemConfigManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ItemConfigManager();
                }

                return instance;
            }
        }

        public ItemConfig GetItem(int id)
        {
            itemById.TryGetValue(id, out ItemConfig itemConfig);
            return itemConfig;
        }

        public bool TryGetItem(int id, out ItemConfig itemConfig)
        {
            return itemById.TryGetValue(id, out itemConfig);
        }

        public Color GetQualityColor(int quality)
        {
            return qualityConfig != null ? qualityConfig.GetQualityColor(quality) : Color.white;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        private void LoadConfigs()
        {
            ItemRuntimeConfig runtimeConfig =
                Resources.Load<ItemRuntimeConfig>(RuntimeConfigResourcePath);
            qualityConfig = runtimeConfig != null ? runtimeConfig.QualityConfig : null;
            if (runtimeConfig == null || qualityConfig == null)
            {
                Debug.LogError(
                    $"[ItemConfigManager] 未找到运行时配置或品质配置："
                    + $"Resources/{RuntimeConfigResourcePath}");
            }

            ItemConfig[] itemConfigs = Resources.LoadAll<ItemConfig>(ItemResourcePath);
            foreach (ItemConfig itemConfig in itemConfigs)
            {
                if (itemConfig == null)
                {
                    continue;
                }

                if (itemConfig.ItemID <= 0)
                {
                    Debug.LogError($"[ItemConfigManager] 物品 {itemConfig.name} 的 ID 必须大于 0。", itemConfig);
                    continue;
                }

                if (duplicateItemIds.Contains(itemConfig.ItemID))
                {
                    Debug.LogError(
                        $"[ItemConfigManager] 物品 ID {itemConfig.ItemID} 已被标记为重复：{itemConfig.name}。",
                        itemConfig);
                    continue;
                }

                if (itemById.TryGetValue(itemConfig.ItemID, out ItemConfig existing))
                {
                    itemById.Remove(itemConfig.ItemID);
                    duplicateItemIds.Add(itemConfig.ItemID);
                    Debug.LogError(
                        $"[ItemConfigManager] 物品 ID {itemConfig.ItemID} 重复并已禁用：{existing.name} / {itemConfig.name}。",
                        itemConfig);
                    continue;
                }

                itemById.Add(itemConfig.ItemID, itemConfig);
            }
        }
    }
}

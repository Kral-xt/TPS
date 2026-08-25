using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.ItemSystem;
using UnityEngine;

namespace TPS.ItemSystem.Infrastructure
{
    public sealed class InventoryItemConfigProvider : IItemConfigProvider
    {
        private readonly HashSet<int> missingItemIDs = new HashSet<int>();

        public bool TryGetItem(int itemID, out InventoryItemConfigData itemConfig)
        {
            if (ItemConfigManager.Instance.TryGetItem(itemID, out ItemConfig sourceConfig)
                && sourceConfig != null)
            {
                InventoryItemCategory categories = InventoryItemCategory.None;
                IReadOnlyList<ItemType> itemTypes = sourceConfig.ItemTypes;
                int typeCount = itemTypes?.Count ?? 0;
                for (int index = 0; index < typeCount; index++)
                {
                    categories |= ConvertCategory(itemTypes[index]);
                }

                itemConfig = new InventoryItemConfigData(
                    sourceConfig.ItemID,
                    sourceConfig.ItemIcon,
                    sourceConfig.ItemName,
                    sourceConfig.Description,
                    sourceConfig.Quality,
                    ItemConfigManager.Instance.GetQualityColor(sourceConfig.Quality),
                    categories);
                return true;
            }

            itemConfig = null;
            if (missingItemIDs.Add(itemID))
            {
                Debug.LogWarning($"[InventoryItemConfigProvider] 未找到物品配置，已跳过显示。ItemID={itemID}");
            }

            return false;
        }

        private InventoryItemCategory ConvertCategory(ItemType itemType)
        {
            switch (itemType)
            {
                case ItemType.Equipment:
                    return InventoryItemCategory.Equipment;
                case ItemType.Material:
                    return InventoryItemCategory.Material;
                case ItemType.Fragment:
                    return InventoryItemCategory.Fragment;
                case ItemType.Other:
                    return InventoryItemCategory.Other;
                case ItemType.Usable:
                    return InventoryItemCategory.Usable;
                default:
                    return InventoryItemCategory.None;
            }
        }
    }
}

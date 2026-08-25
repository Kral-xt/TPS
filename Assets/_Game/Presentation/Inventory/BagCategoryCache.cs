using System;
using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.Inventory.Application;

namespace TPS.Inventory.Presentation
{
    public sealed class BagCategoryCache
    {
        private static readonly InventoryItemCategory[] Categories =
        {
            InventoryItemCategory.Equipment,
            InventoryItemCategory.Material,
            InventoryItemCategory.Fragment,
            InventoryItemCategory.Other,
            InventoryItemCategory.Usable
        };

        private readonly List<InventoryDisplayItem> allItems =
            new List<InventoryDisplayItem>();
        private readonly Dictionary<InventoryItemCategory, List<InventoryDisplayItem>>
            categoryItems =
                new Dictionary<InventoryItemCategory, List<InventoryDisplayItem>>();

        public BagCategoryCache()
        {
            for (int index = 0; index < Categories.Length; index++)
            {
                categoryItems.Add(
                    Categories[index],
                    new List<InventoryDisplayItem>());
            }
        }

        public int TotalCount => allItems.Count;

        public void Rebuild(IReadOnlyList<InventoryDisplayItem> source)
        {
            allItems.Clear();
            foreach (List<InventoryDisplayItem> items in categoryItems.Values)
            {
                items.Clear();
            }

            if (source == null)
            {
                return;
            }

            for (int itemIndex = 0; itemIndex < source.Count; itemIndex++)
            {
                InventoryDisplayItem item = source[itemIndex];
                if (item == null || item.Count <= 0)
                {
                    continue;
                }

                AddRepeated(allItems, item);
                for (int categoryIndex = 0; categoryIndex < Categories.Length; categoryIndex++)
                {
                    InventoryItemCategory category = Categories[categoryIndex];
                    if (item.HasCategory(category))
                    {
                        AddRepeated(categoryItems[category], item);
                    }
                }
            }
        }

        public IReadOnlyList<InventoryDisplayItem> GetItems(
            InventoryItemCategory? category)
        {
            if (!category.HasValue)
            {
                return allItems;
            }

            return categoryItems.TryGetValue(
                category.Value,
                out List<InventoryDisplayItem> items)
                    ? items
                    : Array.Empty<InventoryDisplayItem>();
        }

        private static void AddRepeated(
            List<InventoryDisplayItem> target,
            InventoryDisplayItem item)
        {
            for (int countIndex = 0; countIndex < item.Count; countIndex++)
            {
                target.Add(item);
            }
        }
    }
}

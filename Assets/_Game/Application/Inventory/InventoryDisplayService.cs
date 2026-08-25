using System;
using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.Inventory.Domain;
using UnityEngine;

namespace TPS.Inventory.Application
{
    public sealed class InventoryDisplayItem
    {
        public InventoryDisplayItem(
            int itemID,
            int count,
            Sprite icon,
            int quality,
            Color qualityColor,
            InventoryItemCategory categories)
            : this(
                itemID,
                count,
                icon,
                string.Empty,
                string.Empty,
                quality,
                qualityColor,
                categories)
        {
        }

        public InventoryDisplayItem(
            int itemID,
            int count,
            Sprite icon,
            string name,
            string description,
            int quality,
            Color qualityColor,
            InventoryItemCategory categories)
        {
            ItemID = itemID;
            Count = count;
            Icon = icon;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Quality = quality;
            QualityColor = qualityColor;
            Categories = categories;
        }

        public int ItemID { get; }
        public int Count { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Description { get; }
        public int Quality { get; }
        public Color QualityColor { get; }
        public InventoryItemCategory Categories { get; }

        public bool HasCategory(InventoryItemCategory category)
        {
            return (Categories & category) != 0;
        }
    }

    public sealed class InventoryDisplayService
    {
        private readonly PlayerInventoryService inventoryService;
        private readonly IItemConfigProvider itemConfigProvider;

        public InventoryDisplayService(
            PlayerInventoryService inventoryService,
            IItemConfigProvider itemConfigProvider)
        {
            this.inventoryService = inventoryService
                ?? throw new ArgumentNullException(nameof(inventoryService));
            this.itemConfigProvider = itemConfigProvider
                ?? throw new ArgumentNullException(nameof(itemConfigProvider));
        }

        public IReadOnlyList<InventoryDisplayItem> GetItems()
        {
            List<InventoryDisplayItem> displayItems = new List<InventoryDisplayItem>();
            IReadOnlyList<ItemData> inventoryItems = inventoryService.ItemEntries;
            for (int index = 0; index < inventoryItems.Count; index++)
            {
                ItemData item = inventoryItems[index];
                if (item == null
                    || item.Count <= 0
                    || !itemConfigProvider.TryGetItem(
                        item.ItemID,
                        out InventoryItemConfigData itemConfig)
                    || itemConfig == null)
                {
                    continue;
                }

                displayItems.Add(new InventoryDisplayItem(
                    item.ItemID,
                    item.Count,
                    itemConfig.Icon,
                    itemConfig.Name,
                    itemConfig.Description,
                    itemConfig.Quality,
                    itemConfig.QualityColor,
                    itemConfig.Categories));
            }

            return displayItems.AsReadOnly();
        }

        public bool HasItemConfig(int itemID)
        {
            return itemID > 0
                && itemConfigProvider.TryGetItem(itemID, out InventoryItemConfigData itemConfig)
                && itemConfig != null;
        }
    }
}

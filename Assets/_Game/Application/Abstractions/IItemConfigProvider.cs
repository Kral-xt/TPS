using System;
using UnityEngine;

namespace TPS.Application.Abstractions
{
    [Flags]
    public enum InventoryItemCategory
    {
        None = 0,
        Equipment = 1 << 0,
        Material = 1 << 1,
        Fragment = 1 << 2,
        Other = 1 << 3,
        Usable = 1 << 4
    }

    public sealed class InventoryItemConfigData
    {
        public InventoryItemConfigData(
            int itemID,
            Sprite icon,
            int quality,
            Color qualityColor,
            InventoryItemCategory categories)
            : this(
                itemID,
                icon,
                string.Empty,
                string.Empty,
                quality,
                qualityColor,
                categories)
        {
        }

        public InventoryItemConfigData(
            int itemID,
            Sprite icon,
            string name,
            string description,
            int quality,
            Color qualityColor,
            InventoryItemCategory categories)
        {
            ItemID = itemID;
            Icon = icon;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Quality = quality;
            QualityColor = qualityColor;
            Categories = categories;
        }

        public int ItemID { get; }
        public Sprite Icon { get; }
        public string Name { get; }
        public string Description { get; }
        public int Quality { get; }
        public Color QualityColor { get; }
        public InventoryItemCategory Categories { get; }
    }

    public interface IItemConfigProvider
    {
        bool TryGetItem(int itemID, out InventoryItemConfigData itemConfig);
    }
}

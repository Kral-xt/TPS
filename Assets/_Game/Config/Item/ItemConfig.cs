using System.Collections.Generic;
using UnityEngine;

namespace TPS.ItemSystem
{
    [CreateAssetMenu(fileName = "NewItemConfig", menuName = "Game/Item Config")]
    public class ItemConfig : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField, Min(1)] private int itemID;
        [SerializeField] private Sprite itemIcon;
        [SerializeField] private string itemName;
        [SerializeField, TextArea(3, 8)] private string description;

        [Header("物品品阶")]
        [SerializeField, Range(1, 5)] private int quality = 5;

        [Header("物品类型（支持多选）")]
        [SerializeField] private List<ItemType> itemTypes = new List<ItemType>();

        public int ItemID => itemID;
        public Sprite ItemIcon => itemIcon;
        public string ItemName => itemName;
        public string Description => description;
        public int Quality => quality;
        public IReadOnlyList<ItemType> ItemTypes => itemTypes;

        public bool HasType(ItemType itemType)
        {
            return itemTypes != null && itemTypes.Contains(itemType);
        }

        protected virtual void OnValidate()
        {
            quality = Mathf.Clamp(quality, 1, 5);
            itemName = itemName?.Trim();
            description = description?.Trim();

            if (itemTypes == null)
            {
                itemTypes = new List<ItemType>();
                return;
            }

            for (int index = itemTypes.Count - 1; index >= 0; index--)
            {
                if (itemTypes.IndexOf(itemTypes[index]) != index)
                {
                    itemTypes.RemoveAt(index);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPS.ItemSystem
{
    [Serializable]
    public sealed class ItemQualityColorEntry
    {
        [SerializeField, Range(1, 5)] private int quality = 1;
        [SerializeField] private string displayName = "White";
        [SerializeField] private Color color = Color.white;

        public int Quality => quality;
        public string DisplayName => displayName;
        public Color Color => color;
    }

    [CreateAssetMenu(fileName = "ItemQualityConfig", menuName = "Game/Item Quality Config")]
    public sealed class ItemQualityConfig : ScriptableObject
    {
        [Header("品阶颜色")]
        [SerializeField] private List<ItemQualityColorEntry> qualityColors = new List<ItemQualityColorEntry>();

        [Header("无效品阶回退")]
        [SerializeField] private string fallbackDisplayName = "Unknown";
        [SerializeField] private Color fallbackColor = Color.white;

        private Dictionary<int, ItemQualityColorEntry> qualityLookup;

        public Color GetQualityColor(int quality)
        {
            return TryGetEntry(quality, out ItemQualityColorEntry entry) ? entry.Color : fallbackColor;
        }

        public string GetQualityDisplayName(int quality)
        {
            return TryGetEntry(quality, out ItemQualityColorEntry entry)
                ? entry.DisplayName
                : fallbackDisplayName;
        }

        private void OnEnable()
        {
            RebuildLookup();
        }

        private void OnValidate()
        {
            RebuildLookup();
        }

        private bool TryGetEntry(int quality, out ItemQualityColorEntry entry)
        {
            if (qualityLookup == null)
            {
                RebuildLookup();
            }

            return qualityLookup.TryGetValue(quality, out entry);
        }

        private void RebuildLookup()
        {
            qualityLookup = new Dictionary<int, ItemQualityColorEntry>();
            if (qualityColors == null)
            {
                return;
            }

            foreach (ItemQualityColorEntry entry in qualityColors)
            {
                if (entry == null)
                {
                    continue;
                }

                if (qualityLookup.ContainsKey(entry.Quality))
                {
                    Debug.LogError($"[ItemQualityConfig] 品阶 {entry.Quality} 存在重复颜色配置。", this);
                    continue;
                }

                qualityLookup.Add(entry.Quality, entry);
            }
        }
    }
}

using UnityEngine;

namespace TPS.ItemSystem
{
    [CreateAssetMenu(fileName = "ItemRuntimeConfig", menuName = "Game/Item Runtime Config")]
    public sealed class ItemRuntimeConfig : ScriptableObject
    {
        [Header("品质颜色配置")]
        [SerializeField] private ItemQualityConfig qualityConfig;

        public ItemQualityConfig QualityConfig => qualityConfig;
    }
}

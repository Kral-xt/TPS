using TPS.Infrastructure.Config;
using UnityEngine;

namespace TPS.Player.Infrastructure
{
    public static class PlayerConfigProvider
    {
        public static PlayerConfig Load()
        {
            PlayerConfig config = GameConfigManager.Resolve()?.PlayerConfig;
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<PlayerConfig>();
            config.hideFlags = HideFlags.DontSave;
            Debug.LogWarning(
                "[PlayerConfig] GameConfigManager 未绑定玩家配置，当前使用内存默认值。");
            return config;
        }
    }
}

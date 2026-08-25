using TPS.Infrastructure.Config;
using UnityEngine;

namespace TPS.CameraSystem.Infrastructure
{
    public static class CameraConfigProvider
    {
        public static CameraConfig Load()
        {
            CameraConfig config = GameConfigManager.Resolve()?.CameraConfig;
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<CameraConfig>();
            config.hideFlags = HideFlags.DontSave;
            Debug.LogWarning(
                "[CameraConfig] GameConfigManager 未绑定镜头配置，当前使用内存默认值。");
            return config;
        }
    }
}

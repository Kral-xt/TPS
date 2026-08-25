using UnityEngine;
using TPS.Infrastructure.Config;

namespace TPS.BulletTime.Infrastructure
{
    [CreateAssetMenu(fileName = "BulletTimeConfig", menuName = "TPS/Bullet Time/Config")]
    public sealed class BulletTimeConfig : ScriptableObject
    {
        [Header("能量")]
        [SerializeField, Min(1f)] private float maxEnergy = 100f;
        [SerializeField, Min(0f)] private float consumePerSecond = 20f;
        [SerializeField, Min(0f)] private float lowEnergyThreshold = 60f;
        [SerializeField, Range(0f, 1f)] private float lowRangeRecoveryPercentPerSecond = 0.10f;
        [SerializeField, Range(0f, 1f)] private float highRangeRecoveryPercentPerSecond = 0.20f;
        [SerializeField, Range(0f, 1f)] private float killRecoveryPercent = 0.30f;
        [SerializeField, Min(0f)] private float minimumActivationEnergy = 10f;
        [SerializeField, Min(0f)] private float recoveryDelay = 0.5f;

        [Header("精准闪避子弹时间能量")]
        [SerializeField, Min(1f)] private float dodgeBulletTimeMaxEnergy = 30f;
        [SerializeField, Min(0f)] private float perfectDodgeEnergy = 30f;
        [SerializeField, Min(0f)] private float dodgeBulletTimeConsumePerSecond = 20f;

        [Header("普通闪避消耗")]
        [Tooltip("每次普通闪避消耗的常驻子弹时间能量")]
        [SerializeField, Min(0f)] private float dodgeCost = 10f;

        [Header("时间缩放")]
        [SerializeField, Range(0.01f, 1f)] private float timeScale = 0.3f;
        [SerializeField, Min(0.01f)] private float enterTransitionDuration = 0.15f;
        [SerializeField, Min(0.01f)] private float exitTransitionDuration = 0.25f;

        [Header("视角")]
        [SerializeField, Range(0.01f, 1f)] private float bulletTimeLookSensitivityMultiplier = 0.5f;
        [SerializeField, Min(0.01f)] private float lookSensitivityTransitionDuration = 0.2f;

        [Header("视觉")]
        [SerializeField, Range(-100f, 0f)] private float saturation = -28f;
        [SerializeField] private Color colorFilter = new Color(0.78f, 0.9f, 1f, 1f);
        [SerializeField, Range(0f, 1f)] private float vignetteIntensity = 0.22f;
        [SerializeField, Range(0f, 1f)] private float motionBlurIntensity = 0.12f;
        [SerializeField, Min(0.01f)] private float depthOfFieldFocusDistance = 8f;
        [SerializeField, Range(1f, 32f)] private float depthOfFieldAperture = 6f;

        public float MaxEnergy => maxEnergy;
        public float ConsumePerSecond => consumePerSecond;
        public float LowEnergyThreshold => lowEnergyThreshold;
        public float LowRangeRecoveryPercentPerSecond => lowRangeRecoveryPercentPerSecond;
        public float HighRangeRecoveryPercentPerSecond => highRangeRecoveryPercentPerSecond;
        public float KillRecoveryPercent => killRecoveryPercent;
        public float MinimumActivationEnergy => minimumActivationEnergy;
        public float RecoveryDelay => recoveryDelay;

        // 精准闪避子弹时间
        public float DodgeBulletTimeMaxEnergy => dodgeBulletTimeMaxEnergy;
        public float PerfectDodgeEnergy => perfectDodgeEnergy;
        public float DodgeBulletTimeConsumePerSecond => dodgeBulletTimeConsumePerSecond;

        // 普通闪避消耗
        public float DodgeCost => dodgeCost;

        // 时间缩放与视角
        public float TimeScale => timeScale;
        public float EnterTransitionDuration => enterTransitionDuration;
        public float ExitTransitionDuration => exitTransitionDuration;
        public float BulletTimeLookSensitivityMultiplier => bulletTimeLookSensitivityMultiplier;
        public float LookSensitivityTransitionDuration => lookSensitivityTransitionDuration;
        public float Saturation => saturation;
        public Color ColorFilter => colorFilter;
        public float VignetteIntensity => vignetteIntensity;
        public float MotionBlurIntensity => motionBlurIntensity;
        public float DepthOfFieldFocusDistance => depthOfFieldFocusDistance;
        public float DepthOfFieldAperture => depthOfFieldAperture;
        private void OnValidate()
        {
            lowEnergyThreshold = Mathf.Clamp(lowEnergyThreshold, 0f, maxEnergy);
            minimumActivationEnergy = Mathf.Clamp(minimumActivationEnergy, 0f, maxEnergy);
            dodgeBulletTimeMaxEnergy = Mathf.Max(1f, dodgeBulletTimeMaxEnergy);
            perfectDodgeEnergy = Mathf.Clamp(perfectDodgeEnergy, 0f, dodgeBulletTimeMaxEnergy);
            dodgeBulletTimeConsumePerSecond = Mathf.Max(0f, dodgeBulletTimeConsumePerSecond);
            dodgeCost = Mathf.Max(0f, dodgeCost);
        }
    }

    public static class BulletTimeConfigProvider
    {


        public static BulletTimeConfig Load()
        {
            BulletTimeConfig config = GameConfigManager.Resolve()?.BulletTimeConfig;
            if (config != null)
            {
                return config;
            }

            config = ScriptableObject.CreateInstance<BulletTimeConfig>();
            config.hideFlags = HideFlags.DontSave;
            Debug.LogError("[BulletTime] GameConfigManager 未绑定子弹时间配置，当前使用内存默认值。");
            return config;
        }
    }
}

using TPS.Audio.Infrastructure;
using TPS.BulletTime.Infrastructure;
using TPS.CameraSystem;
using TPS.CameraSystem.Infrastructure;
using TPS.Enemy.Presentation;
using TPS.Player.Infrastructure;
using UnityEngine;

namespace TPS.Infrastructure.Config
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    public sealed class GameConfigManager : MonoBehaviour
    {
        private static GameConfigManager current;

        [Header("武器")]
        [SerializeField] private WeaponConfig weaponConfig;

        [Header("音频")]
        [SerializeField] private AudioConfig audioConfig;
        [SerializeField] private KillAudioConfig killAudioConfig;

        [Header("玩家")]
        [SerializeField] private PlayerConfig playerConfig;

        [Header("敌人")]
        [SerializeField] private EnemyConfig enemyConfig;

        [Header("子弹时间")]
        [SerializeField] private BulletTimeConfig bulletTimeConfig;

        [Header("镜头")]
        [SerializeField] private CameraConfig cameraConfig;
        [SerializeField] private CameraFeedbackProfile cameraFeedbackProfile;

        public static GameConfigManager Current => current;
        public AudioConfig AudioConfig => audioConfig;
        public KillAudioConfig KillAudioConfig => killAudioConfig;
        public WeaponConfig WeaponConfig => weaponConfig;
        public PlayerConfig PlayerConfig => playerConfig;
        public EnemyConfig EnemyConfig => enemyConfig;
        public BulletTimeConfig BulletTimeConfig => bulletTimeConfig;
        public CameraConfig CameraConfig => cameraConfig;
        public CameraFeedbackProfile CameraFeedbackProfile => cameraFeedbackProfile;

        public static GameConfigManager Resolve()
        {
            if (current == null)
            {
                current = FindFirstObjectByType<GameConfigManager>();
            }

            return current;
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Debug.LogError("[GameConfigManager] 场景中存在重复配置管理器。", this);
                enabled = false;
                return;
            }

            current = this;
            ValidateReferences();
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }
        }

        private void ValidateReferences()
        {
            if (audioConfig == null
                || killAudioConfig == null
                || weaponConfig == null
                || playerConfig == null
                || enemyConfig == null
                || bulletTimeConfig == null
                || cameraConfig == null
                || cameraFeedbackProfile == null)
            {
                Debug.LogError("[GameConfigManager] 配置引用不完整，请检查 BattleRuntimeBootstrap。", this);
            }
        }

    }
}

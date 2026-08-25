using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [CreateAssetMenu(
        fileName = "RangedZombieEnemyConfig",
        menuName = "TPS/Enemy/Ranged Zombie Config")]
    public sealed class RangedZombieEnemyConfig : EnemyConfig
    {
        [Header("远程感知")]
        [SerializeField, InspectorName("索敌距离"), Min(0f)]
        private float detectionRange = 30f;

        [Header("弹道")]
        [SerializeField, InspectorName("子弹速度"), Min(0.01f)]
        private float bulletSpeed = 20f;

        [SerializeField, InspectorName("子弹判定半径"), Min(0.01f)]
        private float bulletHitRadius = 0.2f;

        [SerializeField, InspectorName("子弹最大存活时间"), Min(0.1f)]
        private float bulletLifetime = 5f;

        [SerializeField, InspectorName("动画事件备用延迟"), Min(0f)]
        private float attackReleaseFallbackDelay = 0.75f;

        [SerializeField, InspectorName("可触发精准闪避")]
        private bool canTriggerDodge = true;

        public float DetectionRange => detectionRange;
        public float BulletSpeed => bulletSpeed;
        public float BulletHitRadius => bulletHitRadius;
        public float BulletLifetime => bulletLifetime;
        public float AttackReleaseFallbackDelay => attackReleaseFallbackDelay;
        public bool CanTriggerDodge => canTriggerDodge;

        protected override void OnValidate()
        {
            base.OnValidate();
            detectionRange = Mathf.Max(0f, detectionRange);
            bulletSpeed = Mathf.Max(0.01f, bulletSpeed);
            bulletHitRadius = Mathf.Max(0.01f, bulletHitRadius);
            bulletLifetime = Mathf.Max(0.1f, bulletLifetime);
            attackReleaseFallbackDelay = Mathf.Max(0f, attackReleaseFallbackDelay);
        }
    }
}

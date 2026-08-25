using TPS.Combat.Application;
using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class RangedZombieEnemyController : MonoBehaviour
    {
        private const float TargetSearchInterval = 0.5f;
        private const int BulletPoolInitialCapacity = 12;
        private const int BulletPoolMaxCapacity = 64;
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [Header("配置")]
        [SerializeField, InspectorName("远程敌人配置")]
        private RangedZombieEnemyConfig config;

        [Header("引用")]
        [SerializeField, InspectorName("生命组件")]
        private EnemyHealth health;

        [SerializeField, InspectorName("模型根节点")]
        private Transform visualRoot;

        [SerializeField, InspectorName("动画器")]
        private Animator animator;

        [SerializeField, InspectorName("发射点")]
        private Transform muzzleTransform;

        [SerializeField, InspectorName("玩家射击特效 Prefab")]
        private GameObject bulletVisualPrefab;

        private Transform target;
        private float nextTargetSearchTime;
        private float nextAttackTime;
        private float pendingReleaseTime;
        private long pendingAttackId;
        private bool shotPending;

        private void Awake()
        {
            ResolveReferences();
            if (!ValidateConfiguration())
            {
                enabled = false;
                return;
            }

            animator.applyRootMotion = false;
            animator.SetFloat(SpeedHash, 0f);
            EnsureAnimationEventReceiver();
            InitializeBulletPool();
        }

        private void OnEnable()
        {
            target = null;
            nextTargetSearchTime = 0f;
            nextAttackTime = 0f;
            shotPending = false;
            pendingAttackId = 0L;
        }

        private void OnDisable()
        {
            shotPending = false;
            target = null;
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                shotPending = false;
                return;
            }

            AcquireTarget();
            if (target == null)
            {
                shotPending = false;
                return;
            }

            RotateVisualToTarget();
            if (shotPending && Time.time >= pendingReleaseTime)
            {
                ReleaseProjectile();
            }

            if (!shotPending && Time.time >= nextAttackTime)
            {
                StartAttack();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = IsTargetInRange(newTarget) ? newTarget : null;
        }

        public void DealAttackDamage()
        {
            ReleaseProjectile();
        }

        private void ResolveReferences()
        {
            health ??= GetComponent<EnemyHealth>();
            animator ??= GetComponentInChildren<Animator>(true);
            if (visualRoot == null && animator != null)
            {
                visualRoot = animator.transform;
            }

            if (muzzleTransform == null)
            {
                Transform existingMuzzle = transform.Find("Muzzle");
                muzzleTransform = existingMuzzle;
            }
        }

        private bool ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError(
                    "[RangedZombieEnemyController] 未绑定远程敌人配置。",
                    this);
                return false;
            }

            if (health == null || animator == null || visualRoot == null)
            {
                Debug.LogError(
                    "[RangedZombieEnemyController] 生命组件、Animator 或模型根节点缺失。",
                    this);
                return false;
            }

            if (muzzleTransform == null || bulletVisualPrefab == null)
            {
                Debug.LogError(
                    "[RangedZombieEnemyController] 发射点或玩家射击特效 Prefab 未绑定。",
                    this);
                return false;
            }

            return true;
        }

        private void InitializeBulletPool()
        {
            PoolManager.EnsureRuntimeInstance().InitializePool(new PoolConfig
            {
                type = PoolObjectType.Bullet,
                prefab = bulletVisualPrefab,
                initialCapacity = BulletPoolInitialCapacity,
                maxCapacity = BulletPoolMaxCapacity
            });
        }

        private void EnsureAnimationEventReceiver()
        {
            EnemyAnimationEventRelay relay = animator.GetComponent<EnemyAnimationEventRelay>();
            if (relay == null)
            {
                relay = animator.gameObject.AddComponent<EnemyAnimationEventRelay>();
            }

            relay.SetRangedController(this);
        }

        private void AcquireTarget()
        {
            if (IsTargetInRange(target))
            {
                return;
            }

            target = null;
            if (Time.time < nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime = Time.time + TargetSearchInterval;
            try
            {
                GameObject player = GameObject.FindGameObjectWithTag(config.targetTag);
                if (player != null && IsTargetInRange(player.transform))
                {
                    target = player.transform;
                }
            }
            catch (UnityException)
            {
                target = null;
            }
        }

        private bool IsTargetInRange(Transform candidate)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy || config == null)
            {
                return false;
            }

            Vector3 offset = candidate.position - transform.position;
            float detectionRange = config.DetectionRange;
            return offset.sqrMagnitude <= detectionRange * detectionRange;
        }

        private void RotateVisualToTarget()
        {
            Vector3 direction = target.position - visualRoot.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion desiredRotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up);
            float smoothFactor = 1f - Mathf.Exp(
                -Mathf.Max(0f, config.rotationSpeed) * Time.deltaTime);
            visualRoot.rotation = Quaternion.Slerp(
                visualRoot.rotation,
                desiredRotation,
                smoothFactor);
        }

        private void StartAttack()
        {
            shotPending = true;
            pendingAttackId = CombatRuntimeEvents.CreateAttackId();
            pendingReleaseTime = Time.time + config.AttackReleaseFallbackDelay;
            nextAttackTime = Time.time + Mathf.Max(0.01f, config.attackCooldown);
            animator.SetTrigger(AttackHash);
        }

        private void ReleaseProjectile()
        {
            if (!shotPending || target == null || !IsTargetInRange(target))
            {
                shotPending = false;
                return;
            }

            PoolManager poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                shotPending = false;
                return;
            }

            GameObject bulletObject = poolManager.GetObject(PoolObjectType.Bullet);
            if (bulletObject == null)
            {
                shotPending = false;
                return;
            }

            RangedBullet bullet = bulletObject.GetComponent<RangedBullet>();
            if (bullet == null)
            {
                bullet = bulletObject.AddComponent<RangedBullet>();
            }

            Vector3 origin = muzzleTransform.position;
            Vector3 targetPoint = ResolveTargetPoint(target);
            bullet.Configure(
                origin,
                targetPoint - origin,
                config.BulletSpeed,
                config.attackDamage,
                config.BulletHitRadius,
                config.BulletLifetime,
                config.CanTriggerDodge,
                transform,
                target,
                pendingAttackId);
            shotPending = false;
        }

        private static Vector3 ResolveTargetPoint(Transform targetTransform)
        {
            Collider targetCollider = targetTransform.GetComponentInChildren<Collider>();
            return targetCollider != null
                ? targetCollider.bounds.center
                : targetTransform.position + Vector3.up;
        }
    }
}

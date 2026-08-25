using TPS.Combat.Application;
using TPS.Combat.Domain;
using UnityEngine;
using UnityEngine.AI;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class EnemyController : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] private EnemyConfig config;
        [SerializeField] private EnemyHealth health;
        
        [SerializeField] private EnemyAttackWarningPresenter attackWarningPresenter;
[SerializeField] private Animator animator;
        private string targetTag;
        [SerializeField] private NavMeshAgent agent;

        private CharacterController characterController;
        private Transform target;
        private float attackTimer;
        private float nextTargetSearchTime;
        private bool attackStarted;
        private long currentAttackId;
        private float currentAttackStartTime;
        private Vector3 verticalVelocity;
        private float nextPathRefreshTime;
        private Vector3 lastPathTargetPosition;

        private bool CanUseAgent => agent != null && agent.enabled && agent.isOnNavMesh;

        private void Awake()
        {
            ResolveReferences();
            if (config == null)
            {
                Debug.LogError("[EnemyController] EnemyConfig is not assigned.", this);
                enabled = false;
                return;
            }

            
            attackWarningPresenter ??= GetComponent<EnemyAttackWarningPresenter>();
            attackWarningPresenter ??= gameObject.AddComponent<EnemyAttackWarningPresenter>();
            attackWarningPresenter.Configure(config);
targetTag = config.targetTag;
            ConfigureAgent();
            EnsureAnimationEventReceiver();
            EnsureHitbox();
        }

private void OnEnable()
        {
            nextTargetSearchTime = 0f;
            attackTimer = 0f;
            attackStarted = false;
            currentAttackId = 0L;
            currentAttackStartTime = 0f;
        }

private void OnDisable()
        {
            attackWarningPresenter?.Hide();
            StopAgent();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                StopAgent();
                SetSpeed(0f);
                return;
            }

            attackTimer = Mathf.Max(0f, attackTimer - Time.deltaTime);
            if (attackTimer <= 0f)
            {
                attackStarted = false;
            }

            AcquireTarget();
            TickMovementAndAttack();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }


public void DealAttackDamage()
        {
            attackWarningPresenter?.Hide();
            if (health != null && health.IsDead)
            {
                return;
            }

            if (!IsTargetUsable(target))
            {
                AcquireTarget(true);
            }

            if (!IsTargetUsable(target))
            {
                return;
            }

            float attackRange = config.attackRange;
            float tolerance = config.attackEventHitTolerance;
            Vector3 offset = target.position - transform.position;
            offset.y = 0f;
            float hitRange = attackRange + tolerance;
            if (offset.sqrMagnitude > hitRange * hitRange)
            {
                return;
            }

            if (offset.sqrMagnitude > 0.0001f)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                float facingDot = config.attackFacingDot;
                if (Vector3.Dot(forward, offset.normalized) < facingDot)
                {
                    return;
                }
            }

            IDamageable damageable = ResolveDamageable(target);
            if (damageable == null || damageable.IsDead)
            {
                return;
            }

            float damage = config.attackDamage;
            DamageInfo damageInfo = new DamageInfo(damage, DamageSourceKind.Other, this);
            if (damageable is IIdentifiedAttackDamageable identifiedDamageable)
            {
                identifiedDamageable.ApplyDamage(damageInfo, currentAttackId);
            }
            else if (damageable is IAttributedDamageable attributedDamageable)
            {
                attributedDamageable.ApplyDamage(damageInfo);
            }
            else
            {
                damageable.ApplyDamage(damage);
            }
        }

        private void ResolveReferences()
        {
            health ??= GetComponent<EnemyHealth>();
            animator ??= GetComponentInChildren<Animator>(true);
            characterController ??= GetComponent<CharacterController>();
            agent ??= GetComponent<NavMeshAgent>();
        }

        private void ConfigureAgent()
        {
            if (agent == null || config == null)
            {
                return;
            }

            agent.speed = config.moveSpeed;
            agent.stoppingDistance = config.attackRange;
            agent.updateRotation = false;
        }

        private void EnsureAnimationEventReceiver()
        {
            if (animator == null)
            {
                return;
            }

            EnemyAnimationEventRelay relay = animator.GetComponent<EnemyAnimationEventRelay>();
            if (relay == null)
            {
                relay = animator.gameObject.AddComponent<EnemyAnimationEventRelay>();
            }

            relay.SetController(this);
        }

        private void EnsureHitbox()
        {
            if (GetComponent<EnemyHitbox>() == null)
            {
                gameObject.AddComponent<EnemyHitbox>();
            }
        }

        private void AcquireTarget(bool immediate = false)
        {
            if (IsTargetUsable(target) && IsTargetInRange(target))
            {
                return;
            }

            target = null;
            if (!immediate && Time.time < nextTargetSearchTime)
            {
                return;
            }

            nextTargetSearchTime = Time.time + 1f;
            try
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag(targetTag);
                if (playerObject != null && IsTargetInRange(playerObject.transform))
                {
                    target = playerObject.transform;
                }
            }
            catch (UnityException)
            {
                target = null;
            }
        }

        private bool IsTargetInRange(Transform candidate)
        {
            if (!IsTargetUsable(candidate))
            {
                return false;
            }

            float radius = config.searchRadius;
            return (candidate.position - transform.position).sqrMagnitude <= radius * radius;
        }

        private static bool IsTargetUsable(Transform candidate)
        {
            return candidate != null && candidate.gameObject.activeInHierarchy;
        }

        private void TickMovementAndAttack()
        {
            if (target == null)
            {
                StopAgent();
                if (!CanUseAgent)
                {
                    ApplyGravityOnly();
                }

                SetSpeed(0f);
                return;
            }

            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            RotateToTarget(toTarget);

            float attackRange = config.attackRange;
            if (distance <= attackRange)
            {
                StopAgent();
                if (!CanUseAgent)
                {
                    ApplyGravityOnly();
                }

                SetSpeed(0f);
                TryStartAttack();
                return;
            }

            if (CanUseAgent)
            {
                TickAgentMovement();
            }
            else
            {
                TickFallbackMovement(toTarget, distance);
            }
        }

        private void TickAgentMovement()
        {
            agent.isStopped = false;
            bool shouldRepath = Time.time >= nextPathRefreshTime;
            float threshold = config.repathTargetMoveDistance;
            if (!shouldRepath && Vector3.Distance(target.position, lastPathTargetPosition) > threshold)
            {
                shouldRepath = true;
            }

            if (shouldRepath)
            {
                agent.SetDestination(target.position);
                nextPathRefreshTime = Time.time + config.pathRefreshInterval;
                lastPathTargetPosition = target.position;
            }

            SetSpeed(agent.velocity.magnitude);
        }

        private void TickFallbackMovement(Vector3 toTarget, float distance)
        {
            if (characterController == null || !characterController.enabled)
            {
                SetSpeed(0f);
                return;
            }

            Vector3 direction = distance > 0.001f ? toTarget / distance : Vector3.zero;
            float moveSpeed = config.moveSpeed;
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            Vector3 motion = direction * moveSpeed;
            motion.y = verticalVelocity.y;
            characterController.Move(motion * Time.deltaTime);

            if (characterController.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }

            SetSpeed(moveSpeed);
        }

        private void RotateToTarget(Vector3 toTarget)
        {
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion desired = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            float rotationSpeed = config.rotationSpeed;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                desired,
                Mathf.Clamp01(rotationSpeed * Time.deltaTime));
        }

        private void ApplyGravityOnly()
        {
            if (characterController == null || !characterController.enabled)
            {
                return;
            }

            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            characterController.Move(verticalVelocity * Time.deltaTime);
            if (characterController.isGrounded && verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f;
            }
        }

private void TryStartAttack()
        {
            if (attackStarted || attackTimer > 0f)
            {
                return;
            }

            attackStarted = true;
            attackTimer = config.attackCooldown;
            currentAttackId = CombatRuntimeEvents.CreateAttackId();
            currentAttackStartTime = Time.time;

            float attackRange = config.attackRange;
            float tolerance = config.attackEventHitTolerance;
            float activeWindow = config.attackActiveWindow;
            float facingDot = config.attackFacingDot;
            attackWarningPresenter?.Show(config.attackPrepareTime);

            
CombatRuntimeEvents.PublishEnemyAttackStarted(new EnemyAttackStartedEvent(
                currentAttackId,
                this,
                transform.position,
                transform.forward,
                attackRange + tolerance,
                currentAttackStartTime,
                activeWindow,
                facingDot));

            animator?.SetTrigger(AttackHash);
        }

        private void StopAgent()
        {
            if (!CanUseAgent)
            {
                return;
            }

            agent.isStopped = true;
        }

        private static IDamageable ResolveDamageable(Transform candidate)
        {
            Transform current = candidate;
            while (current != null)
            {
                MonoBehaviour[] behaviours = current.GetComponents<MonoBehaviour>();
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IDamageable damageable)
                    {
                        return damageable;
                    }
                }

                current = current.parent;
            }

            return null;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (target != null && hit.transform == target)
            {
                TryStartAttack();
            }
        }

        private void SetSpeed(float value)
        {
            animator?.SetFloat(SpeedHash, value);
        }
    }
}

using System.Collections;
using TPS.Application.Abstractions;
using TPS.Combat.Application;
using TPS.Combat.Domain;
using TPS.Combat.Presentation;

using UnityEngine;
using UnityEngine.AI;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    /// <summary>
    /// 敌人生命值管理组件
    /// 处理敌人受到伤害、死亡动画、音效播放等逻辑
    /// </summary>
    public sealed class EnemyHealth : MonoBehaviour, IAttributedDamageable, IHitPartResolver
    {
        // 动画参数哈希值
        private static readonly int AttackHash = Animator.StringToHash("Attack");
        private static readonly int DeadHash = Animator.StringToHash("Dead");
        private static readonly int IdleHash = Animator.StringToHash("Idle");
        // 死亡动画过渡时间
        private const float DeathTransitionDuration = 0.1f;
        // 生命值低于30%时播放破损音效
        private const float BrokenHealthThreshold = 0.3f;


        [SerializeField] private EnemyConfig config;           // 敌人配置数据
        [SerializeField] private Animator animator;           // 动画控制器
        [SerializeField] private EnemyHitFlash hitFlash;      // 受击闪烁组件
        [SerializeField, InspectorName("当前生命值")] private float currentHP;  // 当前生命值


        private CharacterController characterController;  // 角色控制器
        private Collider[] gameplayColliders;             // 游戏相关碰撞体数组
        private NavMeshAgent agent;                       // 导航代理
        private bool dead;                                // 是否已死亡
        private bool brokenAudioPlayed;                   // 破损音效是否已播放
        private bool deathAudioPlayed;                    // 死亡音效是否已播放
        private Transform visualRoot;                     // 视觉根节点
        private Vector3 initialVisualLocalPosition;       // 初始视觉位置
        private Coroutine deathRoutine;                   // 死亡协程
        private AnimatorCullingMode initialAnimatorCullingMode;  // 初始动画剔除模式


        /// <summary>
        /// 是否已死亡
        /// </summary>
        public bool IsDead => dead;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float CurrentHP => currentHP;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHP => config != null ? config.maxHP : 0f;

        private void Awake()
        {
            ResolveReferences();
            if (config == null)
            {
                Debug.LogError("[EnemyHealth] EnemyConfig is not assigned.", this);
                enabled = false;
                return;
            }

            ResetHealth();
        }

        private void OnValidate()
        {
            ResolveReferences();  // 编辑器模式下解析引用
        }

        /// <summary>
        /// 重置生命值和状态
        /// </summary>
        public void ResetHealth()
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
                deathRoutine = null;
            }

            dead = false;
            brokenAudioPlayed = false;
            deathAudioPlayed = false;
            currentHP = MaxHP;
            if (visualRoot != null)
            {
                visualRoot.localPosition = initialVisualLocalPosition;
            }

            if (animator != null)
            {
                animator.cullingMode = initialAnimatorCullingMode;
            }

            SetGameplayEnabled(true);
            hitFlash?.ResetFlash();
            SetAnimatorDead(false);
        }

        /// <summary>
        /// 应用伤害（IDamageable接口实现）
        /// </summary>
        /// <param name="amount">伤害值</param>
        public void ApplyDamage(float amount)
        {
            ApplyDamage(new DamageInfo(amount, DamageSourceKind.Unknown, null));
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            ApplyDamageInternal(damageInfo);
        }

        public void TakeDamage(float amount)
        {
            ApplyDamage(new DamageInfo(amount, DamageSourceKind.Unknown, null));
        }

        public void TakeDamage(float amount, bool isCritical)
        {
            ApplyDamage(new DamageInfo(amount, DamageSourceKind.Unknown, null, isCritical));
        }

        public bool TryResolveHitPart(object hitObject, out HitPartInfo hitPart)
        {
            hitPart = default;
            if (hitObject is not Collider hitCollider
                || hitCollider.transform == null
                || !hitCollider.transform.IsChildOf(transform))
            {
                return false;
            }

            Transform current = hitCollider.transform;
            while (current != null && current != transform)
            {
                if (current.name == "HeadShot")
                {
                    float bonusCriticalChance = config != null
                        ? config.headShotBonusCriticalChance
                        : 0f;
                    hitPart = new HitPartInfo(
                        HitPartType.HeadShot,
                        Mathf.Clamp01(bonusCriticalChance));
                    return true;
                }

                current = current.parent;
            }

            if (hitCollider.isTrigger)
            {
                return false;
            }

            hitPart = new HitPartInfo(HitPartType.Body, 0f);
            return true;
        }
        private void ApplyDamageInternal(DamageInfo damageInfo)
        {
            if (dead)
            {
                return;
            }

            float damage = Mathf.Max(0f, damageInfo.Amount);
            if (damage <= 0f)
            {
                return;
            }

            if (damageInfo.IsCritical)
            {
                CombatFloatingTextManager.ShowCriticalDamage(transform, damage);
            }
            else
            {
                CombatFloatingTextManager.ShowDamage(transform, damage);
            }

            currentHP = Mathf.Max(0f, currentHP - damage);
            hitFlash?.Play();

            if (currentHP <= 0f)
            {
                Die(damageInfo);
                return;
            }

            TryPlayBrokenAudio();
        }
        /// <summary>
        /// 死亡处理
        /// </summary>
private void Die(DamageInfo damageInfo)
        {
            if (dead)
            {
                return;
            }

            dead = true;
            if (damageInfo.Source == DamageSourceKind.Player && damageInfo.Instigator != null)
            {
                CombatEventHub.PublishEnemyKilled(new EnemyKilledEvent(
                    this,
                    damageInfo.Source,
                    damageInfo.Instigator,
                    damageInfo.HitPart == HitPartType.HeadShot));

                int experienceReward = config != null
                    ? Mathf.CeilToInt(config.experienceValue)
                    : 0;
                CombatRuntimeEvents.PublishEnemyExperienceRewarded(
                    new EnemyExperienceRewardedEvent(
                        this,
                        damageInfo.Source,
                        damageInfo.Instigator,
                        experienceReward));
            }

            PlayDeathAudio();
            SetGameplayEnabled(false);

            if (animator != null)
            {
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }

            SetAnimatorDead(true);
            deathRoutine = StartCoroutine(DeathSequence());
        }

        /// <summary>
        /// 死亡序列协程
        /// 等待死亡动画播放完毕后销毁对象
        /// </summary>
        private IEnumerator DeathSequence()
        {
            const float maximumWaitTime = 5f;  // 最大等待时间，防止无限等待
            float elapsed = 0f;

            while (animator != null && elapsed < maximumWaitTime)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName("Dead") && state.normalizedTime >= 1f)
                {
                    break;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            deathRoutine = null;
            Destroy(gameObject);  // 销毁敌人对象
        }


        /// <summary>
        /// 解析组件引用
        /// 自动获取所需的组件，避免空引用
        /// </summary>
        private void ResolveReferences()
        {
            animator ??= GetComponentInChildren<Animator>(true);
            hitFlash ??= GetComponent<EnemyHitFlash>();
            characterController ??= GetComponent<CharacterController>();
            agent ??= GetComponent<NavMeshAgent>();
            gameplayColliders = GetComponentsInChildren<Collider>(true);

            // 初始化视觉根节点
            if (visualRoot == null && animator != null)
            {
                visualRoot = animator.transform;
                initialVisualLocalPosition = visualRoot.localPosition;
                initialAnimatorCullingMode = animator.cullingMode;
            }

            // 确保存在浮动文本锚点
            if (GetComponent<EnemyFloatingTextAnchor>() == null)
            {
                gameObject.AddComponent<EnemyFloatingTextAnchor>();
            }
        }

        /// <summary>
        /// 尝试播放破损音效
        /// 当生命值首次降至30%及以下时播放
        /// </summary>
        private void TryPlayBrokenAudio()
        {
            if (brokenAudioPlayed || currentHP > MaxHP * BrokenHealthThreshold)
            {
                return;
            }

            brokenAudioPlayed = true;
            GameAudio.Current?.PlayEnemyBrokenSound();
        }

        /// <summary>
        /// 播放死亡音效
        /// </summary>
        private void PlayDeathAudio()
        {
            if (deathAudioPlayed)
            {
                return;
            }

            deathAudioPlayed = true;
            GameAudio.Current?.PlayEnemyDownSound();
        }

        /// <summary>
        /// 设置游戏相关组件的启用状态
        /// </summary>
        /// <param name="value">是否启用</param>
        private void SetGameplayEnabled(bool value)
        {
            if (characterController != null)
            {
                characterController.enabled = value;
            }

            if (gameplayColliders != null)
            {
                for (int i = 0; i < gameplayColliders.Length; i++)
                {
                    if (gameplayColliders[i] != null)
                    {
                        gameplayColliders[i].enabled = value;
                    }
                }
            }

            if (agent == null)
            {
                return;
            }

            if (!value && agent.enabled)
            {
                if (agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                agent.enabled = false;
            }
            else if (value && !agent.enabled)
            {
                agent.enabled = true;
            }
        }

        /// <summary>
        /// 设置动画器死亡状态
        /// </summary>
        /// <param name="value">是否死亡</param>
        private void SetAnimatorDead(bool value)
        {
            if (animator == null)
            {
                return;
            }

            if (HasParameter(animator, AttackHash))
            {
                animator.ResetTrigger(AttackHash);
            }

            if (HasParameter(animator, DeadHash))
            {
                animator.SetBool(DeadHash, value);
            }

            if (value)
            {
                animator.CrossFadeInFixedTime(DeadHash, DeathTransitionDuration, 0, 0f);
            }
            else
            {
                animator.Play(IdleHash, 0, 0f);
            }
        }

        /// <summary>
        /// 检查动画器是否包含指定参数
        /// </summary>
        /// <param name="target">目标动画器</param>
        /// <param name="hash">参数哈希值</param>
        /// <returns>是否包含该参数</returns>
        private static bool HasParameter(Animator target, int hash)
        {
            AnimatorControllerParameter[] parameters = target.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

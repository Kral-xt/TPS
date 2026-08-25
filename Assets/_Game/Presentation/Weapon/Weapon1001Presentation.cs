using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.CameraSystem;

using TPS.Combat.Application;
using TPS.Combat.Domain;
using TPS.Infrastructure.Config;
using TPS.Player.Application;
using TPS.Player.Domain;
using TPS.Player.Presentation;
using TPS.Weapon;
using TPS.Weapon.Application;
using UnityEngine;

/// <summary>
/// 武器1001表现层脚本
/// 负责武器的瞄准、射击逻辑和特效展示
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class Weapon1001Presentation : MonoBehaviour
{


    // 特效与表面的偏移距离
    private const float SurfaceOffset = 0.01f;
    // 击中特效对象池初始容量
    private const int HitVfxInitialCapacity = 12;
    // 击中特效对象池最大容量
    private const int HitVfxMaxCapacity = 64;
    // 连续射击时待结算命中的预分配容量
    private const int PendingHitInitialCapacity = 8;

    private static readonly int FireStateHash = Animator.StringToHash("Base Layer.1001Fire");

    [Header("开火表现")]
    [Tooltip("武器模型使用的 Animator")]
    [SerializeField]
    private Animator weaponAnimator;

    [Tooltip("负责播放池化枪口特效的控制器")]
    [SerializeField]
    private WeaponFireController fireController;

    [Tooltip("视觉子弹的发射起点，用于计算命中特效延迟")]
    [SerializeField]
    private Transform muzzleTransform;

    [Header("位置跟随")]
    [Tooltip("武器需要跟随位置的玩家根节点")]
    [SerializeField]
    private Transform followTarget;

    [Tooltip("独立于玩家层级的武器视图根节点")]
    [SerializeField]
    private Transform weaponViewRoot;

    [Header("瞄准设置")]
    [Tooltip("武器跟随相机方向的旋转平滑速度，值越大响应越快")]
    [SerializeField]
    private float rotationSmooth = 20f;

    // 武器配置数据
    private WeaponConfig _config;
    // 射击处理器
    private WeaponShootHandler _shootHandler;
    // 主摄像机
    private Camera _gameCamera;
    // 模型瞄准旋转偏移量（用于修正武器朝向）
    private Quaternion _modelAimRotationOffset;
    // 武器模型导入时的基础局部旋转
    private PlayerCombatAttributeController _combatAttributes;
    private Quaternion _baseLocalRotation;
    // 不包含开火动画偏移的世界空间平滑瞄准旋转
    private Quaternion _smoothedAimWorldRotation;
    // WeaponViewRoot 相对玩家的初始世界空间位置偏移
    private Vector3 _followWorldOffset;
    
    private bool _isTargetingEnemy;
private bool _hasValidFollowBinding;

    private CameraFeedbackManager _feedbackManager;
    private readonly List<PendingHit> _pendingHits = new(PendingHitInitialCapacity);

    private readonly struct PendingHit
    {
        public PendingHit(RaycastHit hit, float impactTime)
        {
            Hit = hit;
            ImpactTime = impactTime;
        }

        public RaycastHit Hit { get; }
        public float ImpactTime { get; }
    }


    /// <summary>
    /// 初始化方法
    /// 计算模型瞄准偏移、加载配置、初始化射击处理器和特效池
    /// </summary>
    private void Awake()
    {
        InitializePositionFollow();

        _baseLocalRotation = transform.localRotation;
        Quaternion baseWorldRotation = transform.rotation;
        _smoothedAimWorldRotation = baseWorldRotation;

        // 模型枪管默认指向本地左侧，视觉顶部为本地后方。
        Vector3 baseBarrelDirection = baseWorldRotation * Vector3.left;
        Vector3 baseModelUp = baseWorldRotation * Vector3.back;
        Quaternion baseAimFrame = Quaternion.LookRotation(
            baseBarrelDirection,
            baseModelUp);
        _modelAimRotationOffset = Quaternion.Inverse(baseAimFrame)
            * baseWorldRotation;

        weaponAnimator ??= GetComponent<Animator>();
        fireController ??= GetComponent<WeaponFireController>();

        if (weaponAnimator == null)
        {
            Debug.LogWarning("[Weapon1001Presentation] 未绑定武器 Animator，开火动画将不会播放。", this);
        }

        if (fireController == null)
        {
            Debug.LogWarning("[Weapon1001Presentation] 未绑定 WeaponFireController，枪口特效将不会播放。", this);
        }

        _config = GameConfigManager.Resolve()?.WeaponConfig;
        if (_config == null)
        {
            Debug.LogError(
                "[Weapon1001Presentation] GameConfigManager 未绑定武器配置。",
                this);
            enabled = false;
            return;
        }

        _gameCamera = Camera.main;
        _shootHandler = new WeaponShootHandler(_config, ResolveShootOwnerRoot());
        InitializeHitVfxPool();
        _feedbackManager = CameraFeedbackManager.Resolve();
    }

    /// <summary>
    /// 每帧更新
    /// 处理射击输入和击中特效生成
    /// </summary>
    private void Update()
    {
        if (IsOwnerDead()) return;
        ResolvePendingHits();

        if (_shootHandler == null
            || !TryResolveCamera()
            || !PlayerInputGate.IsGameplay
            || !Input.GetMouseButton(0))
        {
            return;
        }

        if (!_shootHandler.TryShoot(_gameCamera, out RaycastHit hit))
        {
            return;
        }

        PlayFirePresentation();

        _feedbackManager ??= CameraFeedbackManager.Resolve();
        _feedbackManager?.PlayShoot();

        if (hit.collider != null)
        {
            QueuePendingHit(hit);
        }
    }

private void OnDisable()
    {
        _pendingHits.Clear();
        _isTargetingEnemy = false;
        CombatPresentationEvents.PublishCrosshairTargetChanged(false);
    }

    /// <summary>
    /// 延迟更新
    /// 处理武器瞄准动画（确保在摄像机更新后执行）
    /// </summary>
    private void LateUpdate()
    {
        UpdateFollowPosition();

        if (_shootHandler == null || _config == null)
        {
            _config = GameConfigManager.Resolve()?.WeaponConfig;
            if (_config != null)
            {
                _gameCamera = Camera.main;
                _shootHandler = new WeaponShootHandler(_config, ResolveShootOwnerRoot());
            }
        }

        if (!TryResolveCamera() || _shootHandler == null)
        {
            return;
        }

        // Animator 在 LateUpdate 前写入根节点动画，先提取开火旋转偏移。
        Quaternion animationRotationOffset = Quaternion.identity;
        if (IsFireAnimationActive())
        {
            animationRotationOffset = Quaternion.Inverse(_baseLocalRotation)
                * transform.localRotation;
        }

        Vector3 aimPoint = _shootHandler.GetAimPoint(_gameCamera, out RaycastHit aimHit);
        bool isTargetingEnemy = _shootHandler.IsValidEnemyHit(aimHit);
        if (_isTargetingEnemy != isTargetingEnemy)
        {
            _isTargetingEnemy = isTargetingEnemy;
            CombatPresentationEvents.PublishCrosshairTargetChanged(isTargetingEnemy);
        }

        Vector3 cameraForward = aimPoint - _gameCamera.transform.position;
        if (cameraForward.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        cameraForward.Normalize();

        Vector3 stableUp = Vector3.ProjectOnPlane(
            _gameCamera.transform.up,
            cameraForward);
        if (stableUp.sqrMagnitude <= 0.0001f)
        {
            stableUp = Vector3.ProjectOnPlane(Vector3.up, cameraForward);
        }

        if (stableUp.sqrMagnitude <= 0.0001f)
        {
            stableUp = Vector3.ProjectOnPlane(Vector3.forward, cameraForward);
        }

        Quaternion aimFrame = Quaternion.LookRotation(
            cameraForward,
            stableUp.normalized);
        Quaternion targetAimWorldRotation = aimFrame * _modelAimRotationOffset;

        float smoothFactor = 1f - Mathf.Exp(
            -Mathf.Max(0f, rotationSmooth) * Time.deltaTime);
        _smoothedAimWorldRotation = Quaternion.Slerp(
            _smoothedAimWorldRotation,
            targetAimWorldRotation,
            smoothFactor);

        transform.rotation = _smoothedAimWorldRotation * animationRotationOffset;
    }

    /// <summary>
    /// 缓存独立武器根节点相对玩家的世界空间位置偏移。
    /// </summary>
    private void InitializePositionFollow()
    {
        weaponViewRoot ??= transform.root;

        if (followTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            followTarget = player != null ? player.transform : null;
        }

        _hasValidFollowBinding = followTarget != null
            && weaponViewRoot != null
            && followTarget != weaponViewRoot;

        if (!_hasValidFollowBinding)
        {
            Debug.LogWarning(
                "[Weapon1001Presentation] 玩家或 WeaponViewRoot 未正确绑定，位置跟随已禁用。",
                this);
            return;
        }

        _followWorldOffset = weaponViewRoot.position - followTarget.position;
    }

    /// <summary>
    /// 仅跟随玩家世界位置，不继承玩家旋转。
    /// </summary>
    private void UpdateFollowPosition()
    {
        if (!_hasValidFollowBinding)
        {
            return;
        }

        weaponViewRoot.position = followTarget.position + _followWorldOffset;
    }

    private Transform ResolveShootOwnerRoot()
    {
        return followTarget != null ? followTarget : transform.root;
    }

private PlayerCombatAttributeController ResolveCombatAttributes()
    {
        Transform owner = ResolveShootOwnerRoot();
        if (owner == null)
        {
            return null;
        }

        return owner.GetComponent<PlayerCombatAttributeController>()
            ?? owner.GetComponentInParent<PlayerCombatAttributeController>();
    }


    private bool IsOwnerDead()
    {
        Transform root = transform.root;
        if (root == null) return false;
        PlayerHealthController health = root.GetComponent<PlayerHealthController>();
        return health != null && health.IsDead;
    }

    /// <summary>
    /// 初始化击中特效对象池
    /// </summary>
    private void InitializeHitVfxPool()
    {
        if (_config.WeaponHitVFX == null)
        {
            Debug.LogError("[Weapon1001Presentation] 武器配置中未设置击中特效预制体。", this);
            return;
        }

        // 获取或创建对象池管理器实例
        PoolManager poolManager = PoolManager.EnsureRuntimeInstance();
        // 初始化击中特效池
        poolManager.InitializePool(new PoolConfig
        {
            type = PoolObjectType.HitVFX,
            prefab = _config.WeaponHitVFX,
            initialCapacity = HitVfxInitialCapacity,
            maxCapacity = HitVfxMaxCapacity
        });
    }

    /// <summary>
    /// 根据视觉弹道速度记录命中结算时间。
    /// </summary>
    private void QueuePendingHit(RaycastHit hit)
    {
        Vector3 origin = muzzleTransform != null
            ? muzzleTransform.position
            : transform.position;
        float visualSpeed = Mathf.Max(0.01f, _config.ProjectileVisualSpeed);
        float travelTime = Vector3.Distance(origin, hit.point) / visualSpeed;

        _pendingHits.Add(new PendingHit(hit, Time.time + travelTime));
    }

    /// <summary>
    /// 在视觉子弹到达命中点后播放特效并结算伤害。
    /// </summary>
    private void ResolvePendingHits()
    {
        float currentTime = Time.time;
        for (int i = _pendingHits.Count - 1; i >= 0; i--)
        {
            PendingHit pendingHit = _pendingHits[i];
            if (currentTime < pendingHit.ImpactTime)
            {
                continue;
            }

            _pendingHits.RemoveAt(i);
            SpawnHitVfx(pendingHit.Hit);
            ApplyHitDamage(pendingHit.Hit);
        }
    }

    /// <summary>
    /// 生成击中特效
    /// </summary>
    /// <param name="hit">射线检测结果</param>
    private void SpawnHitVfx(RaycastHit hit)
    {
        if (PoolManager.Instance == null)
        {
            return;
        }

        // 从对象池获取特效对象
        GameObject effect = PoolManager.Instance.GetObject(PoolObjectType.HitVFX);
        if (effect == null)
        {
            return;
        }

        // 设置特效位置（稍微偏离表面）和旋转（朝向法线）
        effect.transform.SetPositionAndRotation(
            hit.point + hit.normal * SurfaceOffset,
            Quaternion.FromToRotation(Vector3.forward, hit.normal));
    }

private void ApplyHitDamage(RaycastHit hit)
    {
        if (hit.collider == null)
        {
            return;
        }

        Component damageComponent = hit.collider.GetComponentInParent(typeof(IDamageable), true);
        if (damageComponent is not IDamageable damageable || damageable.IsDead)
        {
            return;
        }

        bool hitEnemy = _shootHandler != null && _shootHandler.IsValidEnemyHit(hit);
        HitPartInfo hitPart = new(HitPartType.Body, 0f);
        Component hitPartResolverComponent = hit.collider.GetComponentInParent(
            typeof(IHitPartResolver),
            true);
        bool hasHitPart = hitPartResolverComponent is IHitPartResolver hitPartResolver
            && hitPartResolver.TryResolveHitPart(hit.collider, out hitPart);
        float bonusCriticalChance = hasHitPart
            ? hitPart.BonusCriticalChance
            : 0f;

        if (hasHitPart && hitPart.PartType == HitPartType.HeadShot)
        {
            Debug.Log("HeadShot Hit", hit.collider);
        }

        _combatAttributes ??= ResolveCombatAttributes();
        DamageResult result = _combatAttributes != null
            ? _combatAttributes.CalculateDamage(_config.Damage, bonusCriticalChance)
            : PlayerCombatRules.ResolveDamage(
                _config.Damage,
                bonusCriticalChance,
                0f,
                Random.value);

        if (damageable is IAttributedDamageable attributedDamageable)
        {
            attributedDamageable.ApplyDamage(new DamageInfo(
                result.FinalDamage,
                DamageSourceKind.Player,
                ResolveShootOwnerRoot(),
                result.IsCritical,
                hasHitPart ? hitPart.PartType : HitPartType.Body));
        }
        else
        {
            damageable.ApplyDamage(result.FinalDamage);
        }

        if (hitEnemy)
        {
            CombatPresentationEvents.PublishEnemyHit();
        }
    }


    /// <summary>
    /// 尝试解析摄像机
    /// 如果缓存的摄像机为空，尝试重新获取主摄像机
    /// </summary>
    /// <returns>摄像机是否有效</returns>
    private bool TryResolveCamera()
    {
        if (_gameCamera == null)
        {
            _gameCamera = Camera.main;
        }

        return _gameCamera != null;
    }

    /// <summary>
    /// 播放单次开火动画和枪口特效。
    /// </summary>
    private void PlayFirePresentation()
    {
        if (weaponAnimator != null && weaponAnimator.runtimeAnimatorController != null)
        {
            weaponAnimator.Play(FireStateHash, 0, 0f);
        }

        fireController?.Fire();

        GameAudio.Current?.PlayWeaponShotSound();
    }

    /// <summary>
    /// 检查 Animator 当前是否正在驱动开火状态。
    /// </summary>
    private bool IsFireAnimationActive()
    {
        if (weaponAnimator == null || !weaponAnimator.isActiveAndEnabled)
        {
            return false;
        }

        return weaponAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash
            == FireStateHash;
    }
}

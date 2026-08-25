using UnityEngine;
using TPS.VFX;
using TPS.Pooling.Application;

namespace TPS.Weapon
{
    /// <summary>
    /// 武器射击控制器，负责管理射击逻辑和 Fire 特效播放
    /// </summary>
    public class WeaponFireController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField, InspectorName("枪口位置")]
        private Transform muzzleTransform;

        [Header("Fire 特效配置")]
        [SerializeField, InspectorName("FireVFX 预制体")]
        private GameObject fireVfxPrefab;

        [SerializeField, InspectorName("特效持续时间"), Min(0.01f)]
        private float vfxDuration = 0.5f;

        [Header("射击参数")]
        [SerializeField, InspectorName("射击速率"), Min(0.1f)]
        private float fireRate = 0.2f;

        private float _nextFireTime;
        private bool _isInitialized;

        private void Awake()
        {
            InitializePool();
        }

        /// <summary>
        /// 初始化 FireVFX 对象池
        /// </summary>
        private void InitializePool()
        {
            if (fireVfxPrefab == null)
            {
                Debug.LogWarning("[WeaponFireController] FireVFX 预制体未设置", this);
                return;
            }

            var poolManager = PoolManager.EnsureRuntimeInstance();
            
            // 检查是否已初始化
            var config = new PoolConfig
            {
                type = PoolObjectType.WeaponFireVFX,
                prefab = fireVfxPrefab,
                initialCapacity = 5,
                maxCapacity = 20
            };

            poolManager.InitializePool(config);
            _isInitialized = true;
        }

        /// <summary>
        /// 执行射击逻辑
        /// </summary>
        public void Fire()
        {
            if (!CanFire())
            {
                return;
            }

            _nextFireTime = Time.time + fireRate;
            
            // 播放 Fire 特效
            PlayFireVfx();
            
            // TODO: 添加其他射击逻辑（如子弹发射、伤害计算等）
        }

        /// <summary>
        /// 检查是否可以射击
        /// </summary>
        /// <returns>是否可以射击</returns>
        private bool CanFire()
        {
            if (!_isInitialized)
            {
                return false;
            }

            if (Time.time < _nextFireTime)
            {
                return false;
            }

            if (muzzleTransform == null)
            {
                Debug.LogWarning("[WeaponFireController] 枪口位置未设置", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 播放 Fire 特效
        /// </summary>
        private void PlayFireVfx()
        {
            var poolManager = PoolManager.Instance;
            if (poolManager == null)
            {
                Debug.LogError("[WeaponFireController] PoolManager 不存在", this);
                return;
            }

            // 从对象池获取特效对象
            GameObject vfxObject = poolManager.GetObject(PoolObjectType.WeaponFireVFX);
            if (vfxObject == null)
            {
                Debug.LogWarning("[WeaponFireController] 无法获取 FireVFX 对象", this);
                return;
            }

            // 设置特效位置和旋转
            vfxObject.transform.SetPositionAndRotation(
                muzzleTransform.position,
                muzzleTransform.rotation
            );

            // 获取特效组件并播放
            if (vfxObject.TryGetComponent(out WeaponFireVfxPoolBehaviour fireVfx))
            {
                fireVfx.Play(vfxDuration);
            }
            else
            {
                // 如果没有 WeaponFireVfxPoolBehaviour 组件，直接播放粒子
                var particleSystems = vfxObject.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in particleSystems)
                {
                    ps.Play(false);
                }
                
                // 延迟回收
                Destroy(vfxObject, vfxDuration);
            }
        }

        #if UNITY_EDITOR
        [ContextMenu("测试射击")]
        private void TestFire()
        {
            Fire();
        }
        #endif
    }
}
using UnityEngine;

namespace TPS.VFX
{
    /// <summary>
    /// 管理单枚池化武器粒子弹道的播放、命中清理和回收。
    /// </summary>
    public sealed class WeaponFireVfxPoolBehaviour : MonoBehaviour, IPoolableObject
    {
        /// <summary>
        /// 缓存的粒子系统数组
        /// </summary>
        private ParticleSystem[] _particleSystems;
        
        /// <summary>
        /// 池化项组件引用
        /// </summary>
        private PoolItem _poolItem;

        /// <summary>
        /// 唤醒时缓存组件
        /// </summary>
        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>
        /// 从对象池取出时调用
        /// </summary>
        public void OnTakenFromPool()
        {
            CacheComponents();
            StopAndClear();
        }

        /// <summary>
        /// 返回对象池时调用
        /// </summary>
        public void OnReturnedToPool()
        {
            StopAndClear();
        }

        /// <summary>
        /// 播放粒子效果
        /// </summary>
        /// <param name="autoReturnDelay">自动返回对象池的延迟时间</param>
        public void Play(float autoReturnDelay)
        {
            CacheComponents();
            for (var i = 0; i < _particleSystems.Length; i++)
            {
                _particleSystems[i].Play(false);
            }

            if (_poolItem != null)
            {
                _poolItem.AutoReturn(Mathf.Max(0.05f, autoReturnDelay));
            }
        }

        /// <summary>
        /// 在命中点完成粒子效果并回收
        /// </summary>
        public void CompleteAtImpact()
        {
            StopAndClear();
            if (_poolItem != null)
            {
                _poolItem.ReturnToPool();
            }
        }

        /// <summary>
        /// 缓存所需组件
        /// </summary>
        private void CacheComponents()
        {
            if (_poolItem == null)
            {
                _poolItem = GetComponent<PoolItem>();
            }

            if (_particleSystems == null || _particleSystems.Length == 0)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }
        }

        /// <summary>
        /// 停止并清理所有粒子系统
        /// </summary>
        private void StopAndClear()
        {
            if (_particleSystems == null)
            {
                return;
            }

            for (var i = 0; i < _particleSystems.Length; i++)
            {
                _particleSystems[i].Stop(
                    false,
                    ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
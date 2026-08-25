using UnityEngine;

// 应用层和基础设施层共享的定义
/// <summary>对象池对象类型枚举</summary>
public enum PoolObjectType
{
    Enemy,
    HitVFX,
    Bullet,
    WeaponFireVFX,
    PlayerAfterimage,
    EnemyAttackWarning,
}

[System.Serializable]
/// <summary>对象池配置，定义预制体、初始容量和最大容量</summary>
public class PoolConfig
{
    public PoolObjectType type;
    public GameObject prefab;
    public int initialCapacity = 5;
    public int maxCapacity = 50;
}

/// <summary>
/// <summary>
/// 池化对象的生命周期回调接口，用于在取出/归还时重置运行时状态
/// </summary>
/// <summary>池化对象生命周期回调接口，用于在取出/归还时重置运行时状态</summary>
public interface IPoolableObject
{
    void OnTakenFromPool();
    void OnReturnedToPool();
}
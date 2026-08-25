using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponConfig", menuName = "Weapon/WeaponConfig")]
public class WeaponConfig : ScriptableObject
{
    [Header("基础配置")]
    [SerializeField]
    private string weaponName;
    [SerializeField]
    private string weaponType;
    [SerializeField]
    private int weaponLevel;
    [SerializeField]
    private GameObject weaponPrefab;

    [SerializeField] 
    private GameObject weaponHitVFX;
    
    [Header("武器属性")] 
    [SerializeField] 
    private float damage;
    [SerializeField] 
    private float attackSpeed;
    [SerializeField] 
    private float attackRange;

    [Header("视觉弹道")]
    [Tooltip("FireVFX 粒子模拟子弹的飞行速度，用于同步视觉命中与伤害结算。")]
    [SerializeField, Min(0.01f)]
    private float projectileVisualSpeed = 100f;

    [Header("瞄准吸附")]
    [SerializeField, Range(0f, 1f)] private float aimAssistStrength = 0.5f;
    [SerializeField, Min(0f)] private float aimAssistMaxDistance = 30f;
    [SerializeField, Min(0f)] private float aimAssistScreenRadius = 120f;
    [SerializeField, Range(0f, 180f)] private float aimAssistMaxAngle = 8f;
    [SerializeField, Min(0f)] private float aimAssistTargetHoldTime = 0.15f;
    [SerializeField] private LayerMask aimAssistTargetMask = ~0;
    [SerializeField] private LayerMask aimAssistObstacleMask = ~0;
    [SerializeField] private bool showAimAssistDebug;
    
    [Header("武器弹药属性")]
    [SerializeField] 
    private float reloadTime;
    [SerializeField] 
    private int ammoCapacity;
    [SerializeField] 
    private int killAmmoBack;
    [SerializeField] 
    private int ammunitionSupply;

    public string WeaponName => weaponName;
    public string WeaponType => weaponType;
    
    public GameObject WeaponHitVFX => weaponHitVFX;
    public GameObject WeaponPrefab => weaponPrefab;
    public int WeaponLevel => weaponLevel;
    
    public float Damage => damage;
    public float AttackSpeed => attackSpeed;
    public float AttackRange => attackRange;
    
    public float ProjectileVisualSpeed => projectileVisualSpeed;
    public float AimAssistStrength => aimAssistStrength;
    public float AimAssistMaxDistance => aimAssistMaxDistance;
    public float AimAssistScreenRadius => aimAssistScreenRadius;
    public float AimAssistMaxAngle => aimAssistMaxAngle;
    public float AimAssistTargetHoldTime => aimAssistTargetHoldTime;
    public LayerMask AimAssistTargetMask => aimAssistTargetMask;
    public LayerMask AimAssistObstacleMask => aimAssistObstacleMask;
    public bool ShowAimAssistDebug => showAimAssistDebug;
    
    public float ReloadTime => reloadTime;
    public int AmmoCapacity => ammoCapacity;
    public int KillAmmoBack => killAmmoBack;    
    public int AmmunitionSupply => ammunitionSupply;
}
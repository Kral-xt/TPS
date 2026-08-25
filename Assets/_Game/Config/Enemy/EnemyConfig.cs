using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "TPS/Enemy/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [Header("Target")]
        public string targetTag = "Player";

        [Header("属性")]
        [Min(1f)] public float maxHP = 100f;
        [Min(0f)] public float experienceValue = 10f;

        [Header("移动")]
        [Min(0f)] public float moveSpeed = 2.2f;
        [Min(0f)] public float rotationSpeed = 8f;
        [Min(0f)] public float searchRadius = 500f;
        [Min(0f)] public float attackRange = 1.6f;

        [Header("攻击")]
        [Min(0f)] public float attackDamage = 10f;
        [Min(0.01f)] public float attackCooldown = 5f;
        [Min(0f)] public float attackEventHitTolerance = 0.8f;
        [Min(0f)] public float attackActiveWindow = 1f;

        [Header("Weak Point")]
        [Range(0f, 1f)] public float headShotBonusCriticalChance = 0.5f;
        

        [Header("攻击预警")]
        [Min(0.01f)] public float attackPrepareTime = 0.6667f;
        public GameObject attackWarningPrefab;
        public Vector3 attackWarningHeadOffset = new Vector3(0f, 0.25f, 0f);
        [Min(0.0001f)] public float attackWarningWorldScale = 0.005f;
        [Min(0)] public int attackWarningInitialCapacity = 5;
        [Min(1)] public int attackWarningMaxCapacity = 30;
[Range(-1f, 1f)] public float attackFacingDot = 0.1f;

        [Header("寻路")]
        [Min(0.02f)] public float pathRefreshInterval = 0.25f;
        [Min(0f)] public float repathTargetMoveDistance = 0.75f;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(targetTag)) targetTag = "Player";
            maxHP = Mathf.Max(1f, maxHP);
            experienceValue = Mathf.Max(0f, experienceValue);
            moveSpeed = Mathf.Max(0f, moveSpeed);
            rotationSpeed = Mathf.Max(0f, rotationSpeed);
            searchRadius = Mathf.Max(0f, searchRadius);
            attackRange = Mathf.Max(0f, attackRange);
            attackDamage = Mathf.Max(0f, attackDamage);
            attackCooldown = Mathf.Max(0.01f, attackCooldown);
            attackEventHitTolerance = Mathf.Max(0f, attackEventHitTolerance);
            attackActiveWindow = Mathf.Max(0f, attackActiveWindow);
            headShotBonusCriticalChance = Mathf.Clamp01(headShotBonusCriticalChance);
            
            attackPrepareTime = Mathf.Max(0.01f, attackPrepareTime);
            attackWarningWorldScale = Mathf.Max(0.0001f, attackWarningWorldScale);
            attackWarningInitialCapacity = Mathf.Max(0, attackWarningInitialCapacity);
            attackWarningMaxCapacity = Mathf.Max(1, attackWarningMaxCapacity);
            attackWarningInitialCapacity = Mathf.Min(attackWarningInitialCapacity, attackWarningMaxCapacity);
attackFacingDot = Mathf.Clamp(attackFacingDot, -1f, 1f);
            pathRefreshInterval = Mathf.Max(0.02f, pathRefreshInterval);
            repathTargetMoveDistance = Mathf.Max(0f, repathTargetMoveDistance);
        }
    }
}

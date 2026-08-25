#if TPS_ENABLE_COMPANY_PACKAGES
using DamageNumbersPro;
using TPS.Enemy.Presentation;
using UnityEngine;

namespace TPS.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CombatFloatingTextManager : MonoBehaviour
    {
        private const string DamageNumberResourcePath = "DamageNumberWhite";
        private static CombatFloatingTextManager instance;

        [Header("飘字")]
        [SerializeField, InspectorName("头顶偏移")] private float topOffset = 0.35f;
        [SerializeField, InspectorName("对象池大小"), Min(1)] private int poolSize = 160;
        [SerializeField, InspectorName("存活时间"), Min(0.1f)] private float lifetime = 1.2f;
        [SerializeField, InspectorName("缩放"), Min(0.01f)] private float scale = 1f;

        [Header("暴击飘字")]
        [SerializeField, InspectorName("暴击颜色")] private Color criticalColor = Color.yellow;
        [SerializeField, InspectorName("暴击缩放"), Min(0.01f)] private float criticalScale = 1.35f;

        [Header("闪避飘字")]
        [SerializeField, InspectorName("闪避文本")] private string missText = "Miss";
        [SerializeField, InspectorName("闪避颜色")] private Color missColor = new(0.2f, 0.55f, 1f, 1f);
        [SerializeField, InspectorName("闪避缩放"), Min(0.01f)] private float missScale = 1.15f;

        private DamageNumber damageNumberPrefab;

        public static void ShowDamage(Transform target, float damage)
        {
            ResolveManager().ShowNumber(target, damage, Color.white, 1f);
        }

        public static void ShowCriticalDamage(Transform target, float damage)
        {
            CombatFloatingTextManager manager = ResolveManager();
            manager.ShowNumber(target, damage, manager.criticalColor, manager.criticalScale);
        }

        public static void ShowMiss(Transform target)
        {
            CombatFloatingTextManager manager = ResolveManager();
            manager.ShowText(target, manager.missText, manager.missColor, manager.missScale);
        }

        private static CombatFloatingTextManager ResolveManager()
        {
            if (instance != null)
            {
                return instance;
            }

            GameObject managerObject = new("CombatFloatingTextManager");
            instance = managerObject.AddComponent<CombatFloatingTextManager>();
            DontDestroyOnLoad(managerObject);
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsurePrefab();
        }

        private void ShowNumber(Transform target, float damage, Color color, float scaleMultiplier)
        {
            if (target == null || damage <= 0f)
            {
                return;
            }

            DamageNumber prefab = EnsurePrefab();
            if (prefab == null)
            {
                return;
            }

            DamageNumber number = prefab.Spawn(GetTopPosition(target), Mathf.CeilToInt(damage));
            ConfigureNumber(number, color, scale * scaleMultiplier);
        }

        private void ShowText(Transform target, string text, Color color, float scaleMultiplier)
        {
            if (target == null || string.IsNullOrEmpty(text))
            {
                return;
            }

            DamageNumber prefab = EnsurePrefab();
            if (prefab == null)
            {
                return;
            }

            DamageNumber number = prefab.Spawn(GetTopPosition(target), text);
            ConfigureNumber(number, color, scale * scaleMultiplier);
        }

        private static void ConfigureNumber(DamageNumber number, Color color, float targetScale)
        {
            if (number == null)
            {
                return;
            }

            number.cameraOverride = Camera.main != null ? Camera.main.transform : null;
            number.SetColor(color);
            number.SetScale(targetScale);
        }

        private DamageNumber EnsurePrefab()
        {
            if (damageNumberPrefab != null)
            {
                return damageNumberPrefab;
            }

            damageNumberPrefab = Resources.Load<DamageNumber>(DamageNumberResourcePath);
            if (damageNumberPrefab == null)
            {
                Debug.LogWarning($"无法从 Resources/{DamageNumberResourcePath} 加载战斗飘字 Prefab。", this);
                return null;
            }

            damageNumberPrefab.enablePooling = true;
            damageNumberPrefab.poolSize = Mathf.Max(1, poolSize);
            damageNumberPrefab.lifetime = Mathf.Max(0.1f, lifetime);
            damageNumberPrefab.enable3DGame = true;
            damageNumberPrefab.faceCameraView = true;
            damageNumberPrefab.lookAtCamera = false;
            damageNumberPrefab.renderThroughWalls = false;
            damageNumberPrefab.enableNumber = true;
            damageNumberPrefab.enableLeftText = false;
            damageNumberPrefab.enableRightText = false;
            damageNumberPrefab.enableTopText = false;
            damageNumberPrefab.enableBottomText = false;
            damageNumberPrefab.SetColor(Color.white);
            return damageNumberPrefab;
        }

        private Vector3 GetTopPosition(Transform target)
        {
            EnemyFloatingTextAnchor anchor = target.GetComponent<EnemyFloatingTextAnchor>();
            if (anchor != null)
            {
                return anchor.Position;
            }

            CharacterController controller = target.GetComponent<CharacterController>();
            if (controller != null)
            {
                return target.position + controller.center
                    + Vector3.up * (controller.height * 0.5f + topOffset);
            }

            Collider targetCollider = target.GetComponentInChildren<Collider>();
            if (targetCollider != null)
            {
                Bounds bounds = targetCollider.bounds;
                return bounds.center + Vector3.up * (bounds.extents.y + topOffset);
            }

            return target.position + Vector3.up * (2f + topOffset);
        }
    }
}
#else
using UnityEngine;

namespace TPS.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CombatFloatingTextManager : MonoBehaviour
    {
        public static void ShowDamage(Transform target, float damage)
        {
        }

        public static void ShowCriticalDamage(Transform target, float damage)
        {
        }

        public static void ShowMiss(Transform target)
        {
        }
    }
}
#endif

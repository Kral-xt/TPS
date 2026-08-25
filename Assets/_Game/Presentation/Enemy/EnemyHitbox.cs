using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class EnemyHitbox : MonoBehaviour
    {
        [SerializeField, InspectorName("半径"), Min(0.01f)]
        private float radius = 0.55f;

        [SerializeField, InspectorName("高度"), Min(0.02f)]
        private float height = 2.35f;

        [SerializeField, InspectorName("中心")]
        private Vector3 center = new(0f, 1.12f, 0f);

        private CapsuleCollider hitbox;

        private void Awake()
        {
            Configure();
        }

        private void OnValidate()
        {
            Configure();
        }

        private void Configure()
        {
            hitbox ??= GetComponent<CapsuleCollider>();
            if (hitbox == null)
            {
                return;
            }

            hitbox.isTrigger = true;
            hitbox.radius = Mathf.Max(0.01f, radius);
            hitbox.height = Mathf.Max(hitbox.radius * 2f, height);
            hitbox.center = center;
            hitbox.direction = 1;
        }
    }
}

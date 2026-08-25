using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyFloatingTextAnchor : MonoBehaviour
    {
        [SerializeField, InspectorName("头顶偏移")]
        private float topOffset = 0.35f;

        private Vector3 cachedLocalTopOffset = Vector3.up * 2.35f;
        private bool cached;

        public Vector3 Position
        {
            get
            {
                CacheTopOffset();
                return transform.TransformPoint(cachedLocalTopOffset);
            }
        }

        private void Awake()
        {
            CacheTopOffset();
        }

        private void OnValidate()
        {
            cached = false;
            CacheTopOffset();
        }

        private void CacheTopOffset()
        {
            if (cached)
            {
                return;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Bounds bounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = targetRenderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(targetRenderer.bounds);
                }
            }

            if (hasBounds)
            {
                Vector3 topWorld = bounds.center + Vector3.up * (bounds.extents.y + topOffset);
                cachedLocalTopOffset = transform.InverseTransformPoint(topWorld);
            }
            else
            {
                CharacterController controller = GetComponent<CharacterController>();
                cachedLocalTopOffset = controller != null
                    ? controller.center + Vector3.up * (controller.height * 0.5f + topOffset)
                    : Vector3.up * (2f + topOffset);
            }

            cached = true;
        }
    }
}

using UnityEngine;
using UnityEngine.UI;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyAttackWarningPresenter : MonoBehaviour
    {
        private const PoolObjectType PoolType = PoolObjectType.EnemyAttackWarning;

        private EnemyConfig config;
        private EnemyAttackWarningView activeView;
        private Collider[] colliders;
        private Transform headPoint;

        public void Configure(EnemyConfig enemyConfig)
        {
            config = enemyConfig;
            colliders ??= GetComponentsInChildren<Collider>(true);
            headPoint ??= FindHeadPoint(transform);
        }

        public void Show(float duration)
        {
            Hide();

            if (config == null || config.attackWarningPrefab == null)
            {
                return;
            }

            PoolManager manager = PoolManager.EnsureRuntimeInstance();
            manager.InitializePool(new PoolConfig
            {
                type = PoolType,
                prefab = config.attackWarningPrefab,
                initialCapacity = config.attackWarningInitialCapacity,
                maxCapacity = config.attackWarningMaxCapacity
            });

            GameObject warningObject = manager.GetObject(PoolType);
            if (warningObject == null)
            {
                return;
            }

            activeView = warningObject.GetComponent<EnemyAttackWarningView>();
            if (activeView == null)
            {
                activeView = warningObject.AddComponent<EnemyAttackWarningView>();
            }

            activeView.Play(
                transform,
                headPoint,
                colliders,
                config.attackWarningHeadOffset,
                config.attackWarningWorldScale,
                duration);
        }

        public void Hide()
        {
            if (activeView == null)
            {
                return;
            }

            GameObject warningObject = activeView.gameObject;
            activeView.Stop();
            activeView = null;

            PoolManager manager = PoolManager.Instance;
            if (manager != null && warningObject != null && warningObject.activeSelf)
            {
                manager.ReturnObject(PoolType, warningObject);
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        private static Transform FindHeadPoint(Transform root)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == "HeadPoint")
                {
                    return children[i];
                }
            }

            return null;
        }
    }

    [DisallowMultipleComponent]
    internal sealed class EnemyAttackWarningView : MonoBehaviour
    {
        private const float StartScale = 0.5f;

        private Transform enemyRoot;
        private Transform headPoint;
        private Collider[] colliders;
        private Camera gameCamera;
        private Vector3 headOffset;
        private float worldScale;
        private float duration;
        private float elapsed;
        private bool playing;

        public void Play(
            Transform targetRoot,
            Transform targetHeadPoint,
            Collider[] targetColliders,
            Vector3 targetHeadOffset,
            float targetWorldScale,
            float tweenDuration)
        {
            enemyRoot = targetRoot;
            headPoint = targetHeadPoint;
            colliders = targetColliders;
            headOffset = targetHeadOffset;
            worldScale = Mathf.Max(0.0001f, targetWorldScale);
            duration = Mathf.Max(0.01f, tweenDuration);
            elapsed = 0f;
            playing = true;
            gameCamera = Camera.main;

            EnsureWorldCanvas();
            ApplyPose(StartScale);
        }

        public void Stop()
        {
            playing = false;
            enemyRoot = null;
            headPoint = null;
            colliders = null;
            gameCamera = null;
            elapsed = 0f;
        }

        private void LateUpdate()
        {
            if (!playing || enemyRoot == null)
            {
                return;
            }

            elapsed = Mathf.Min(duration, elapsed + Time.deltaTime);
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            ApplyPose(Mathf.Lerp(StartScale, 1f, easedProgress));
        }

        private void ApplyPose(float scaleMultiplier)
        {
            transform.position = ResolveHeadPosition();
            transform.localScale = Vector3.one * (worldScale * scaleMultiplier);

            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }

            if (gameCamera != null)
            {
                transform.rotation = gameCamera.transform.rotation;
            }
        }

        private Vector3 ResolveHeadPosition()
        {
            if (headPoint != null)
            {
                return headPoint.position + headOffset;
            }

            float highestPoint = enemyRoot.position.y;
            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider candidate = colliders[i];
                    if (candidate != null && candidate.enabled)
                    {
                        highestPoint = Mathf.Max(highestPoint, candidate.bounds.max.y);
                    }
                }
            }

            return new Vector3(enemyRoot.position.x, highestPoint, enemyRoot.position.z)
                + headOffset;
        }

        private void EnsureWorldCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = gameCamera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }
}

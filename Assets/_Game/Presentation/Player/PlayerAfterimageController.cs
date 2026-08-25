using TPS.BulletTime.Application;
using TPS.BulletTime.Domain;
using TPS.Player.Infrastructure;
using UnityEngine;

namespace TPS.Player.Presentation
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class PlayerAfterimageController : MonoBehaviour
    {
        private PlayerConfig config;
        private SkinnedMeshRenderer[] sources;
        private Transform characterRoot;
        private Transform renderRoot;
        private PoolManager poolManager;
        private BulletTimeController bulletTimeController;
        private bool poolInitialized;
        private bool dodgeTrailActive;
        private bool bulletTimeTracking;
        private float nextSpawnTime;
        private int lastSpawnFrame = -1;
        private int diagnosticSpawnCount;
        private Vector3 lastBulletTimePosition;
        private Quaternion lastBulletTimeRotation;

        private void Awake()
        {
            config = PlayerConfigProvider.Load();
            ResolveCharacterRenderers();
            InitializePool();
        }

        private void Update()
        {
            if (dodgeTrailActive)
            {
                return;
            }

            if (!IsNormalBulletTimeActive())
            {
                bulletTimeTracking = false;
                return;
            }

            if (!bulletTimeTracking)
            {
                bulletTimeTracking = true;
                lastBulletTimePosition = transform.position;
                lastBulletTimeRotation = transform.rotation;
                nextSpawnTime = Time.unscaledTime + config.BulletTimeAfterimageInterval;
                return;
            }

            float moveThreshold = config.BulletTimeAfterimageMinMoveDistance;
            float rotationThreshold = config.BulletTimeAfterimageMinRotationAngle;
            bool moved = (transform.position - lastBulletTimePosition).sqrMagnitude
                >= moveThreshold * moveThreshold;
            bool rotated = Quaternion.Angle(transform.rotation, lastBulletTimeRotation)
                >= rotationThreshold;
            if (!moved && !rotated)
            {
                return;
            }

            if (SpawnIfDue(
                    config.BulletTimeAfterimageInterval,
                    config.BulletTimeAfterimageLifetime))
            {
                lastBulletTimePosition = transform.position;
                lastBulletTimeRotation = transform.rotation;
            }
        }

        private void OnDisable()
        {
            dodgeTrailActive = false;
            bulletTimeTracking = false;
        }

        public void BeginTrail()
        {
            dodgeTrailActive = true;
            bulletTimeTracking = false;
            diagnosticSpawnCount = 0;
            nextSpawnTime = float.NegativeInfinity;
            if (config.AfterimageScaleDiagnostics)
            {
                Debug.Log(
                    $"[PlayerAfterimageSpawn] Dodge trail started. " +
                    $"Source renderer count: {sources.Length}",
                    this);
            }

            SpawnIfDue(config.AfterimageSpawnInterval, config.AfterimageLifetime);
        }

        public void TickTrail()
        {
            if (dodgeTrailActive)
            {
                SpawnIfDue(config.AfterimageSpawnInterval, config.AfterimageLifetime);
            }
        }

        public void EndTrail()
        {
            dodgeTrailActive = false;
            bulletTimeTracking = false;
            nextSpawnTime = Time.unscaledTime + config.BulletTimeAfterimageInterval;
        }

        private bool IsNormalBulletTimeActive()
        {
            bulletTimeController ??= BulletTimeController.Current;
            return bulletTimeController != null
                && bulletTimeController.CurrentSource == BulletTimeSource.Normal
                && (bulletTimeController.State == BulletTimeState.Entering
                    || bulletTimeController.State == BulletTimeState.Active);
        }

        private void ResolveCharacterRenderers()
        {
            Animator[] animators = GetComponentsInChildren<Animator>(true);
            int bestRendererCount = 0;
            for (int i = 0; i < animators.Length; i++)
            {
                SkinnedMeshRenderer[] animatorRenderers =
                    animators[i].GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (animatorRenderers.Length <= bestRendererCount)
                {
                    continue;
                }

                bestRendererCount = animatorRenderers.Length;
                characterRoot = animators[i].transform;
                sources = animatorRenderers;
            }

            if (sources == null)
            {
                characterRoot = transform;
                sources = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            }
        }

        private void InitializePool()
        {
            poolManager = PoolManager.EnsureRuntimeInstance();
            if (poolManager == null)
            {
                return;
            }

            Transform templateTransform = poolManager.transform.Find("PlayerAfterimageTemplate");
            GameObject template;
            if (templateTransform == null)
            {
                template = new GameObject("PlayerAfterimageTemplate");
                template.transform.SetParent(poolManager.transform, false);
                template.AddComponent<PlayerAfterimagePoolItem>();
                template.SetActive(false);
            }
            else
            {
                template = templateTransform.gameObject;
            }

            poolManager.InitializePool(new PoolConfig
            {
                type = PoolObjectType.PlayerAfterimage,
                prefab = template,
                initialCapacity = config.AfterimageInitialCapacity,
                maxCapacity = config.AfterimageMaxCapacity
            });

            renderRoot = EnsureRenderRoot(poolManager.transform.root);
            poolInitialized = true;
        }

        private static Transform EnsureRenderRoot(Transform runtimeRoot)
        {
            Transform root = runtimeRoot.Find("PlayerAfterimageRenderRoot");
            if (root == null)
            {
                root = new GameObject("PlayerAfterimageRenderRoot").transform;
                root.SetParent(runtimeRoot, false);
            }

            root.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            SetWorldScale(root, Vector3.one);
            return root;
        }

        private bool SpawnIfDue(float interval, float lifetime)
        {
            if (!poolInitialized
                || Time.unscaledTime < nextSpawnTime
                || Time.frameCount == lastSpawnFrame)
            {
                return false;
            }

            nextSpawnTime = Time.unscaledTime + Mathf.Max(0.01f, interval);
            GameObject afterimageObject = poolManager.GetObject(PoolObjectType.PlayerAfterimage);
            if (afterimageObject == null)
            {
                return false;
            }

            afterimageObject.SetActive(true);
            afterimageObject.transform.SetParent(renderRoot, false);
            afterimageObject.transform.localPosition = Vector3.zero;
            afterimageObject.transform.localRotation = Quaternion.identity;
            afterimageObject.transform.localScale = Vector3.one;

            PlayerAfterimagePoolItem afterimage =
                afterimageObject.GetComponent<PlayerAfterimagePoolItem>();
            PoolItem poolItem = afterimageObject.GetComponent<PoolItem>();
            if (afterimage == null || poolItem == null)
            {
                poolManager.ReturnObject(PoolObjectType.PlayerAfterimage, afterimageObject);
                return false;
            }

            if (!afterimage.Capture(
                    sources,
                    config,
                    transform,
                    characterRoot,
                    lifetime))
            {
                poolManager.ReturnObject(PoolObjectType.PlayerAfterimage, afterimageObject);
                return false;
            }

            lastSpawnFrame = Time.frameCount;
            if (config.AfterimageScaleDiagnostics && diagnosticSpawnCount < 8)
            {
                Debug.Log(
                    $"[PlayerAfterimageSpawn] Spawn afterimage: {Time.frameCount}, " +
                    $"active={afterimageObject.activeInHierarchy}, " +
                    $"parent={afterimageObject.transform.parent?.name}",
                    afterimageObject);
            }

            diagnosticSpawnCount++;
            return true;
        }

        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Transform parent = target.parent;
            if (parent == null)
            {
                target.localScale = worldScale;
                return;
            }

            Vector3 parentScale = parent.lossyScale;
            target.localScale = new Vector3(
                DivideScale(worldScale.x, parentScale.x),
                DivideScale(worldScale.y, parentScale.y),
                DivideScale(worldScale.z, parentScale.z));
        }

        private static float DivideScale(float worldScale, float parentScale)
        {
            return Mathf.Abs(parentScale) > 0.0001f
                ? worldScale / parentScale
                : worldScale;
        }
    }
}

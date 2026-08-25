using TPS.Player;
using UnityEngine;

namespace TPS.CameraSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraSpeedLinesEffect : MonoBehaviour
    {
        private const string RuntimeObjectName = "SpeedLinesVFX";

        [Header("引用")]
        [SerializeField, Tooltip("用于读取当前移动速度与方向的玩家控制器")]
        private TpsPrototypePlayerController playerController;

        [Header("触发")]
        [SerializeField, Min(0f), Tooltip("达到该速度时开始显示速度线")]
        private float activationSpeed = 10f;
        [SerializeField, Min(0.01f), Tooltip("达到该速度时速度线进入满强度")]
        private float fullIntensitySpeed = 14f;
        [SerializeField, Range(0f, 1f), Tooltip("刚达到触发速度时的最低可见强度")]
        private float minimumActiveIntensity = 0.3f;
        [SerializeField, Min(0.01f), Tooltip("速度线上升到目标强度所需时间")]
        private float fadeInDuration = 0.18f;
        [SerializeField, Min(0.01f), Tooltip("速度线淡出所需时间")]
        private float fadeOutDuration = 0.28f;

        [Header("粒子")]
        [SerializeField, Range(1f, 100f), Tooltip("满强度时每秒发射数量")]
        private float maximumEmissionRate = 42f;
        [SerializeField, Range(16, 256), Tooltip("粒子系统允许存在的最大粒子数")]
        private int maximumParticles = 96;
        [SerializeField, Min(1f), Tooltip("粒子相对镜头的移动速度")]
        private float particleTravelSpeed = 22f;
        [SerializeField, Min(0.01f), Tooltip("粒子生成平面与镜头的距离")]
        private float spawnDistance = 8f;
        [SerializeField, Min(0.1f), Tooltip("粒子生成环的半径")]
        private float spawnRadius = 2.8f;
        [SerializeField, Range(0f, 1f), Tooltip("生成环厚度，数值越小越靠近屏幕边缘")]
        private float spawnRadiusThickness = 0.65f;
        [SerializeField, Min(0.01f), Tooltip("速度线宽度")]
        private float lineWidth = 0.035f;
        [SerializeField, Min(0.1f), Tooltip("速度线拉伸长度")]
        private float lineLength = 2.8f;
        [SerializeField, Range(0f, 1f), Tooltip("速度线最大透明度")]
        private float lineAlpha = 0.42f;

        private Camera controlledCamera;
        private ParticleSystem speedLines;
        private ParticleSystemRenderer speedLinesRenderer;
        private Material runtimeMaterial;
        private float currentIntensity;
        private float playerSearchTimer;
        private bool emissionActive;

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            ResolvePlayer();
            EnsureParticleSystem();
            ApplyStaticSettings();
            SetEmissionRate(0f);
        }

        private void Update()
        {
            if (playerController == null)
            {
                playerSearchTimer += Time.unscaledDeltaTime;
                if (playerSearchTimer >= 1f)
                {
                    ResolvePlayer();
                }
            }

            float speed = playerController != null ? playerController.CurrentSpeed : 0f;
            float targetIntensity = CalculateTargetIntensity(speed);
            float duration = targetIntensity > currentIntensity
                ? fadeInDuration
                : fadeOutDuration;

            currentIntensity = Mathf.MoveTowards(
                currentIntensity,
                targetIntensity,
                Time.unscaledDeltaTime / Mathf.Max(0.01f, duration));

            UpdateParticleDirection(currentIntensity);
            UpdateShapeForAspectRatio();
            SetEmissionRate(maximumEmissionRate * currentIntensity);
        }

        private void OnDisable()
        {
            currentIntensity = 0f;
            emissionActive = false;
            if (speedLines != null)
            {
                speedLines.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void OnDestroy()
        {
            if (runtimeMaterial != null)
            {
                Destroy(runtimeMaterial);
            }
        }

        private float CalculateTargetIntensity(float speed)
        {
            if (speed < activationSpeed)
            {
                return 0f;
            }

            float speedFactor = Mathf.InverseLerp(
                activationSpeed,
                Mathf.Max(activationSpeed + 0.01f, fullIntensitySpeed),
                speed);
            return Mathf.Lerp(minimumActiveIntensity, 1f, speedFactor);
        }

        private void ResolvePlayer()
        {
            playerController = FindFirstObjectByType<TpsPrototypePlayerController>();
            playerSearchTimer = 0f;
        }

        private void EnsureParticleSystem()
        {
            Transform existing = transform.Find(RuntimeObjectName);
            GameObject effectObject;
            if (existing != null)
            {
                effectObject = existing.gameObject;
            }
            else
            {
                effectObject = new GameObject(RuntimeObjectName);
                effectObject.transform.SetParent(transform, false);
            }

            speedLines = effectObject.GetComponent<ParticleSystem>();
            if (speedLines == null)
            {
                speedLines = effectObject.AddComponent<ParticleSystem>();
            }

            speedLinesRenderer = effectObject.GetComponent<ParticleSystemRenderer>();
        }

        private void ApplyStaticSettings()
        {
            ParticleSystem.MainModule main = speedLines.main;
            main.loop = true;
            main.playOnAwake = false;
            main.prewarm = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Local;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.38f, 0.62f);
            main.startSize = new ParticleSystem.MinMaxCurve(lineWidth * 0.65f, lineWidth);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 1f, 1f, lineAlpha * 0.55f),
                new Color(1f, 1f, 1f, lineAlpha));
            main.maxParticles = maximumParticles;
            main.stopAction = ParticleSystemStopAction.None;

            ParticleSystem.ShapeModule shape = speedLines.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = spawnRadius;
            shape.radiusThickness = spawnRadiusThickness;
            shape.arc = 360f;
            shape.position = new Vector3(0f, 0f, spawnDistance);

            ParticleSystem.EmissionModule emission = speedLines.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;

            ParticleSystem.VelocityOverLifetimeModule velocity = speedLines.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;

            ParticleSystem.NoiseModule noise = speedLines.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = 0.35f;
            noise.strengthY = 0.35f;
            noise.strengthZ = 0.12f;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.35f;
            noise.damping = true;

            speedLinesRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            speedLinesRenderer.velocityScale = 0.08f;
            speedLinesRenderer.lengthScale = lineLength;
            speedLinesRenderer.cameraVelocityScale = 0f;
            speedLinesRenderer.sortMode = ParticleSystemSortMode.Distance;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader != null)
            {
                runtimeMaterial = new Material(shader)
                {
                    name = "Runtime Speed Lines Material",
                    hideFlags = HideFlags.DontSave
                };
                runtimeMaterial.color = Color.white;
                speedLinesRenderer.sharedMaterial = runtimeMaterial;
            }
        }

        private void UpdateParticleDirection(float intensity)
        {
            if (speedLines == null)
            {
                return;
            }

            float activeTravelSpeed = Mathf.Lerp(
                particleTravelSpeed * 0.75f,
                particleTravelSpeed,
                Mathf.Clamp01(intensity));

            ParticleSystem.VelocityOverLifetimeModule velocity = speedLines.velocityOverLifetime;
            velocity.x = 0f;
            velocity.y = 0f;
            velocity.z = -activeTravelSpeed;
        }

        private void UpdateShapeForAspectRatio()
        {
            if (speedLines == null || controlledCamera == null)
            {
                return;
            }

            ParticleSystem.ShapeModule shape = speedLines.shape;
            shape.scale = new Vector3(
                Mathf.Max(1f, controlledCamera.aspect),
                1f,
                1f);
        }

        private void SetEmissionRate(float rate)
        {
            if (speedLines == null)
            {
                return;
            }

            bool shouldEmit = rate > 0.01f;
            if (shouldEmit && !emissionActive)
            {
                speedLines.Play(true);
                emissionActive = true;
            }
            else if (!shouldEmit && emissionActive)
            {
                speedLines.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                emissionActive = false;
            }

            ParticleSystem.EmissionModule emission = speedLines.emission;
            emission.rateOverTime = Mathf.Max(0f, rate);
        }
    }
}

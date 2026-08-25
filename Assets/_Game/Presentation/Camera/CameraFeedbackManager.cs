using System.Collections.Generic;
using TPS.Infrastructure.Config;
using TPS.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TPS.CameraSystem
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public sealed class CameraFeedbackManager : MonoBehaviour
    {

        private const int MaximumActiveFeedbacks = 32;

        private sealed class ActiveFeedback
        {
            public CameraFeedbackPreset preset;
            public Transform lockTarget;
            public float elapsed;
            public float seed;
            public float recoilYaw;
            public bool useCinemachineImpulse;
        }

        private static CameraFeedbackManager current;

        [Header("引用")]
        [SerializeField] private CameraFeedbackProfile profile;
        [SerializeField] private Camera controlledCamera;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private readonly List<ActiveFeedback> activeFeedbacks = new();

        private Transform activeLockTarget;
        private Vector3 activeLockOffset;
        private float activeLockWeight;
        private float activeLockBlendSpeed;

        private float baseTimeScale;
        private float baseFixedDeltaTime;
        private bool ownsTimeScale;

        private DepthOfField depthOfField;
        private MotionBlur motionBlur;
        private bool postProcessCaptured;
        private bool postProcessOverridden;
        private bool originalDepthActive;
        private bool originalMotionBlurActive;
        private DepthOfFieldMode originalDepthMode;
        private float originalFocusDistance;
        private float originalAperture;
        private float originalMotionBlurIntensity;
        private bool originalDepthModeOverride;
        private bool originalFocusOverride;
        private bool originalApertureOverride;
        private bool originalMotionBlurIntensityOverride;


        public static CameraFeedbackManager Current => current;
        public Vector3 LocalPositionOffset { get; private set; }
        public Vector3 RotationOffset { get; private set; }
        public float FovOffset { get; private set; }
        public float FollowDistanceMultiplier { get; private set; } = 1f;
        public float DampingMultiplier { get; private set; } = 1f;

        public static CameraFeedbackManager Resolve()
        {
            if (current != null)
            {
                return current;
            }

            current = FindFirstObjectByType<CameraFeedbackManager>();
            if (current != null)
            {
                return current;
            }

            TpsPrototypeCameraController cameraController =
                FindFirstObjectByType<TpsPrototypeCameraController>();
            GameObject host = cameraController != null
                ? cameraController.gameObject
                : Camera.main != null
                    ? Camera.main.gameObject
                    : new GameObject("CameraFeedbackManager");

            current = host.GetComponent<CameraFeedbackManager>();
            if (current == null)
            {
                current = host.AddComponent<CameraFeedbackManager>();
            }

            return current;
        }

        private void Awake()
        {
            if (current != null && current != this)
            {
                Destroy(this);
                return;
            }

            current = this;
            profile ??= GameConfigManager.Resolve()?.CameraFeedbackProfile;
            controlledCamera ??= GetComponentInChildren<Camera>();
            controlledCamera ??= Camera.main;
            impulseSource ??= GetComponent<CinemachineImpulseSource>();
            if (impulseSource == null)
            {
                impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
            }

            baseTimeScale = Time.timeScale;
            baseFixedDeltaTime = Time.fixedDeltaTime;
        }

        private void Update()
        {
            EvaluateFeedbacks(Time.unscaledDeltaTime);
        }

        private void OnDestroy()
        {
            if (current == this)
            {
                current = null;
            }

            RestoreTimeScale();
            RestorePostProcess();
        }

        public void PlayFeedback(CameraFeedbackType type, Transform lockTarget = null)
        {
            if (profile == null)
            {
                Debug.LogWarning(
                    $"[{nameof(CameraFeedbackManager)}] 未加载默认镜头反馈配置。",
                    this);
                return;
            }

            AddFeedback(profile.GetPreset(type), lockTarget);
        }

        public void PlayShoot()
        {
            PlayFeedback(CameraFeedbackType.Shoot);
        }

        public void PlayHit()
        {
            PlayFeedback(CameraFeedbackType.Hit);
        }

        public void PlayCritical()
        {
            PlayFeedback(CameraFeedbackType.Critical);
        }

        public void PlayDash()
        {
            PlayFeedback(CameraFeedbackType.Dash);
        }

        public void PlayImpulse(CameraImpulseSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.impulse = settings;
            AddFeedback(preset, null);
        }

        public void PlayFov(CameraFovSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.fov = settings;
            AddFeedback(preset, null);
        }

        public void PlayRecoil(CameraRecoilSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.recoil = settings;
            AddFeedback(preset, null);
        }

        public void PlayOffset(CameraOffsetSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.offset = settings;
            AddFeedback(preset, null);
        }

        public void PlayDamping(CameraDampingSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.damping = settings;
            AddFeedback(preset, null);
        }

        public void PlaySlowMotion(CameraTimeScaleSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.timeScale = settings;
            AddFeedback(preset, null);
        }

        public void PlayTilt(CameraTiltSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.tilt = settings;
            AddFeedback(preset, null);
        }

        public void PlayLockTarget(Transform target, CameraLockSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.lockTarget = settings;
            AddFeedback(preset, target);
        }

        public void PlayPostProcess(CameraPostProcessSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.postProcess = settings;
            AddFeedback(preset, null);
        }

        public void PlayZoom(CameraZoomSettings settings)
        {
            CameraFeedbackPreset preset = default;
            preset.zoom = settings;
            AddFeedback(preset, null);
        }

        public void CancelAllFeedback()
        {
            activeFeedbacks.Clear();
            ResetOutputs();
            RestoreTimeScale();
            RestorePostProcess();
        }

        public bool TryGetLockRotation(
            Vector3 origin,
            out Quaternion rotation,
            out float blend)
        {
            rotation = Quaternion.identity;
            blend = 0f;
            if (activeLockTarget == null || activeLockWeight <= 0f)
            {
                return false;
            }

            Vector3 direction = activeLockTarget.TransformPoint(activeLockOffset) - origin;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                return false;
            }

            rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            float response = 1f - Mathf.Exp(
                -Mathf.Max(0.01f, activeLockBlendSpeed) * Time.unscaledDeltaTime);
            blend = Mathf.Clamp01(activeLockWeight * response);
            return true;
        }

        private void AddFeedback(CameraFeedbackPreset preset, Transform lockTarget)
        {
            if (activeFeedbacks.Count >= MaximumActiveFeedbacks)
            {
                activeFeedbacks.RemoveAt(0);
            }

            ActiveFeedback feedback = new()
            {
                preset = preset,
                lockTarget = lockTarget,
                elapsed = 0f,
                seed = Random.Range(0f, 1000f),
                recoilYaw = preset.recoil.enabled
                    ? Random.Range(
                        Mathf.Min(preset.recoil.yawRange.x, preset.recoil.yawRange.y),
                        Mathf.Max(preset.recoil.yawRange.x, preset.recoil.yawRange.y))
                    : 0f
            };

            if (preset.impulse.enabled)
            {
                feedback.useCinemachineImpulse =
                    TryGenerateCinemachineImpulse(preset.impulse);
            }

            activeFeedbacks.Add(feedback);
        }

        private bool TryGenerateCinemachineImpulse(CameraImpulseSettings settings)
        {
            if (impulseSource == null
                || FindFirstObjectByType<CinemachineImpulseListener>(
                    FindObjectsInactive.Include) == null)
            {
                return false;
            }

            if (impulseSource.ImpulseDefinition != null)
            {
                impulseSource.ImpulseDefinition.ImpulseDuration =
                    Mathf.Max(0.01f, settings.duration);
            }

            impulseSource.GenerateImpulse(Mathf.Max(0f, settings.strength));
            return true;
        }

        private void EvaluateFeedbacks(float deltaTime)
        {
            ResetOutputs();

            bool hasTimeScale = false;
            float requestedTimeScale = 1f;
            float postWeight = 0f;
            float focusAccumulator = 0f;
            float apertureAccumulator = 0f;
            float blurAccumulator = 0f;

            for (int i = activeFeedbacks.Count - 1; i >= 0; i--)
            {
                ActiveFeedback feedback = activeFeedbacks[i];
                feedback.elapsed += Mathf.Max(0f, deltaTime);

                if (feedback.elapsed > feedback.preset.Duration)
                {
                    activeFeedbacks.RemoveAt(i);
                    continue;
                }

                EvaluateImpulse(feedback);
                EvaluateFov(feedback);
                EvaluateRecoil(feedback);
                EvaluateOffset(feedback);
                EvaluateDamping(feedback);
                EvaluateTilt(feedback);
                EvaluateZoom(feedback);
                EvaluateLock(feedback);

                if (feedback.preset.timeScale.enabled)
                {
                    float weight =
                        feedback.preset.timeScale.envelope.Evaluate(feedback.elapsed);
                    float scale = Mathf.Lerp(
                        1f,
                        Mathf.Clamp(feedback.preset.timeScale.scale, 0.01f, 1f),
                        weight);
                    requestedTimeScale = Mathf.Min(requestedTimeScale, scale);
                    hasTimeScale |= weight > 0f;
                }

                if (feedback.preset.postProcess.enabled)
                {
                    float weight =
                        feedback.preset.postProcess.envelope.Evaluate(feedback.elapsed);
                    if (weight > 0f)
                    {
                        postWeight += weight;
                        focusAccumulator +=
                            feedback.preset.postProcess.focusDistance * weight;
                        apertureAccumulator +=
                            feedback.preset.postProcess.aperture * weight;
                        blurAccumulator +=
                            feedback.preset.postProcess.motionBlurIntensity * weight;
                    }
                }
            }

            ApplyTimeScale(hasTimeScale, requestedTimeScale);
            ApplyPostProcess(
                postWeight,
                focusAccumulator,
                apertureAccumulator,
                blurAccumulator);
        }

        private void EvaluateImpulse(ActiveFeedback feedback)
        {
            CameraImpulseSettings settings = feedback.preset.impulse;
            if (!settings.enabled || feedback.useCinemachineImpulse)
            {
                return;
            }

            float duration = Mathf.Max(0.01f, settings.duration);
            if (feedback.elapsed > duration)
            {
                return;
            }

            float weight = 1f - Mathf.Clamp01(feedback.elapsed / duration);
            float phase = feedback.elapsed * Mathf.Max(0.01f, settings.frequency);
            Vector3 noise = new(
                SignedPerlin(feedback.seed + 11f, phase),
                SignedPerlin(feedback.seed + 23f, phase),
                SignedPerlin(feedback.seed + 37f, phase));

            LocalPositionOffset += Vector3.Scale(noise, settings.positionAmplitude) * weight;
            RotationOffset += Vector3.Scale(noise, settings.rotationAmplitude) * weight;
        }

        private void EvaluateFov(ActiveFeedback feedback)
        {
            CameraFovSettings settings = feedback.preset.fov;
            if (!settings.enabled)
            {
                return;
            }

            FovOffset += settings.offset * settings.envelope.Evaluate(feedback.elapsed);
        }

        private void EvaluateRecoil(ActiveFeedback feedback)
        {
            CameraRecoilSettings settings = feedback.preset.recoil;
            if (!settings.enabled)
            {
                return;
            }

            float weight = settings.envelope.Evaluate(feedback.elapsed);
            RotationOffset += new Vector3(
                -settings.pitch * weight,
                feedback.recoilYaw * weight,
                0f);
        }

        private void EvaluateOffset(ActiveFeedback feedback)
        {
            CameraOffsetSettings settings = feedback.preset.offset;
            if (!settings.enabled)
            {
                return;
            }

            LocalPositionOffset +=
                settings.localOffset * settings.envelope.Evaluate(feedback.elapsed);
        }

        private void EvaluateDamping(ActiveFeedback feedback)
        {
            CameraDampingSettings settings = feedback.preset.damping;
            if (!settings.enabled)
            {
                return;
            }

            float weight = settings.envelope.Evaluate(feedback.elapsed);
            DampingMultiplier *= Mathf.Lerp(
                1f,
                Mathf.Max(0.01f, settings.multiplier),
                weight);
        }

        private void EvaluateTilt(ActiveFeedback feedback)
        {
            CameraTiltSettings settings = feedback.preset.tilt;
            if (!settings.enabled)
            {
                return;
            }

            RotationOffset += new Vector3(
                0f,
                0f,
                settings.angle * settings.envelope.Evaluate(feedback.elapsed));
        }

        private void EvaluateZoom(ActiveFeedback feedback)
        {
            CameraZoomSettings settings = feedback.preset.zoom;
            if (!settings.enabled)
            {
                return;
            }

            float weight = settings.envelope.Evaluate(feedback.elapsed);
            FollowDistanceMultiplier *= Mathf.Lerp(
                1f,
                Mathf.Max(0.1f, settings.distanceMultiplier),
                weight);
            FovOffset += settings.fovOffset * weight;
        }

        private void EvaluateLock(ActiveFeedback feedback)
        {
            CameraLockSettings settings = feedback.preset.lockTarget;
            if (!settings.enabled || feedback.lockTarget == null)
            {
                return;
            }

            float weight = settings.envelope.Evaluate(feedback.elapsed);
            if (weight <= activeLockWeight)
            {
                return;
            }

            activeLockWeight = weight;
            activeLockTarget = feedback.lockTarget;
            activeLockOffset = settings.targetOffset;
            activeLockBlendSpeed = settings.blendSpeed;
        }

        private void ResetOutputs()
        {
            LocalPositionOffset = Vector3.zero;
            RotationOffset = Vector3.zero;
            FovOffset = 0f;
            FollowDistanceMultiplier = 1f;
            DampingMultiplier = 1f;
            activeLockTarget = null;
            activeLockOffset = Vector3.zero;
            activeLockWeight = 0f;
            activeLockBlendSpeed = 0f;
        }

        private static float SignedPerlin(float seed, float phase)
        {
            return Mathf.PerlinNoise(seed, phase) * 2f - 1f;
        }

        private void ApplyTimeScale(bool hasRequest, float requestedScale)
        {
            if (!hasRequest)
            {
                RestoreTimeScale();
                return;
            }

            if (!ownsTimeScale)
            {
                baseTimeScale = Time.timeScale;
                baseFixedDeltaTime = Time.fixedDeltaTime;
                ownsTimeScale = true;
            }

            float scale = Mathf.Clamp(requestedScale, 0.01f, 1f);
            Time.timeScale = baseTimeScale * scale;
            Time.fixedDeltaTime = baseFixedDeltaTime * scale;
        }

        private void RestoreTimeScale()
        {
            if (!ownsTimeScale)
            {
                return;
            }

            Time.timeScale = baseTimeScale;
            Time.fixedDeltaTime = baseFixedDeltaTime;
            ownsTimeScale = false;
        }

        private void ApplyPostProcess(
            float weight,
            float focusAccumulator,
            float apertureAccumulator,
            float blurAccumulator)
        {
            if (weight <= 0f || !EnsurePostProcessOverrides())
            {
                RestorePostProcess();
                return;
            }

            postProcessOverridden = true;
            float focus = focusAccumulator / weight;
            float aperture = apertureAccumulator / weight;
            float blur = blurAccumulator / weight;

            depthOfField.active = true;
            depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            depthOfField.focusDistance.Override(Mathf.Max(0.01f, focus));
            depthOfField.aperture.Override(Mathf.Clamp(aperture, 1f, 32f));

            motionBlur.active = true;
            motionBlur.intensity.Override(Mathf.Clamp01(blur));
        }

        private bool EnsurePostProcessOverrides()
        {
            if (depthOfField != null && motionBlur != null)
            {
                return true;
            }

            if (postProcessVolume == null)
            {
                Volume[] volumes = FindObjectsByType<Volume>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
                for (int i = 0; i < volumes.Length; i++)
                {
                    if (volumes[i].isGlobal)
                    {
                        postProcessVolume = volumes[i];
                        break;
                    }
                }
            }

            if (postProcessVolume == null)
            {
                return false;
            }

            VolumeProfile runtimeProfile = postProcessVolume.profile;
            if (!runtimeProfile.TryGet(out depthOfField))
            {
                depthOfField = runtimeProfile.Add<DepthOfField>(true);
            }

            if (!runtimeProfile.TryGet(out motionBlur))
            {
                motionBlur = runtimeProfile.Add<MotionBlur>(true);
            }

            if (!postProcessCaptured)
            {
                originalDepthActive = depthOfField.active;
                originalMotionBlurActive = motionBlur.active;
                originalDepthMode = depthOfField.mode.value;
                originalFocusDistance = depthOfField.focusDistance.value;
                originalAperture = depthOfField.aperture.value;
                originalMotionBlurIntensity = motionBlur.intensity.value;
                originalDepthModeOverride = depthOfField.mode.overrideState;
                originalFocusOverride = depthOfField.focusDistance.overrideState;
                originalApertureOverride = depthOfField.aperture.overrideState;
                originalMotionBlurIntensityOverride = motionBlur.intensity.overrideState;

                postProcessCaptured = true;
            }

            return true;
        }

private void RestorePostProcess()
        {
            if (!postProcessCaptured || !postProcessOverridden)
            {
                return;
            }

            depthOfField.active = originalDepthActive;
            depthOfField.mode.value = originalDepthMode;
            depthOfField.mode.overrideState = originalDepthModeOverride;
            depthOfField.focusDistance.value = originalFocusDistance;
            depthOfField.focusDistance.overrideState = originalFocusOverride;
            depthOfField.aperture.value = originalAperture;
            depthOfField.aperture.overrideState = originalApertureOverride;

            motionBlur.active = originalMotionBlurActive;
            motionBlur.intensity.value = originalMotionBlurIntensity;
            motionBlur.intensity.overrideState =
                originalMotionBlurIntensityOverride;
            postProcessOverridden = false;
        }
    }
}

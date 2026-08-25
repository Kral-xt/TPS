using System;
using UnityEngine;

namespace TPS.CameraSystem
{
    public enum CameraFeedbackType
    {
        Shoot,
        Hit,
        Critical,
        Dash
    }

    [Serializable]
    public struct FeedbackEnvelope
    {
        [Min(0f)] public float attack;
        [Min(0f)] public float hold;
        [Min(0f)] public float recovery;

        public float Duration => Mathf.Max(0.001f, attack + hold + recovery);

        public float Evaluate(float elapsed)
        {
            if (elapsed < 0f)
            {
                return 0f;
            }

            if (attack > 0f && elapsed < attack)
            {
                return Mathf.Clamp01(elapsed / attack);
            }

            float holdEnd = attack + hold;
            if (elapsed <= holdEnd)
            {
                return 1f;
            }

            if (recovery <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.Clamp01((elapsed - holdEnd) / recovery);
        }
    }

    [Serializable]
    public struct CameraImpulseSettings
    {
        public bool enabled;
        [InspectorName("位置振幅")] public Vector3 positionAmplitude;
        [InspectorName("旋转振幅")] public Vector3 rotationAmplitude;
        [Min(0.01f), InspectorName("频率")] public float frequency;
        [Min(0.01f), InspectorName("持续时间")] public float duration;
        [Min(0f), InspectorName("Cinemachine强度")] public float strength;
    }

    [Serializable]
    public struct CameraFovSettings
    {
        public bool enabled;
        [InspectorName("FOV增量")] public float offset;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraRecoilSettings
    {
        public bool enabled;
        [Min(0f), InspectorName("垂直后坐")] public float pitch;
        [InspectorName("水平后坐范围")] public Vector2 yawRange;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraOffsetSettings
    {
        public bool enabled;
        [InspectorName("本地位置偏移")] public Vector3 localOffset;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraDampingSettings
    {
        public bool enabled;
        [Min(0.01f), InspectorName("阻尼倍率")] public float multiplier;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraTimeScaleSettings
    {
        public bool enabled;
        [Range(0.01f, 1f), InspectorName("时间倍率")] public float scale;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraTiltSettings
    {
        public bool enabled;
        [InspectorName("倾斜角度")] public float angle;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraLockSettings
    {
        public bool enabled;
        [InspectorName("目标偏移")] public Vector3 targetOffset;
        [Min(0.01f), InspectorName("锁定插值速度")] public float blendSpeed;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraPostProcessSettings
    {
        public bool enabled;
        [Min(0.01f), InspectorName("景深焦点距离")] public float focusDistance;
        [Range(1f, 32f), InspectorName("景深光圈")] public float aperture;
        [Range(0f, 1f), InspectorName("运动模糊强度")] public float motionBlurIntensity;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraZoomSettings
    {
        public bool enabled;
        [Min(0.1f), InspectorName("跟随距离倍率")] public float distanceMultiplier;
        [InspectorName("附加FOV")] public float fovOffset;
        public FeedbackEnvelope envelope;
    }

    [Serializable]
    public struct CameraFeedbackPreset
    {
        public CameraImpulseSettings impulse;
        public CameraFovSettings fov;
        public CameraRecoilSettings recoil;
        public CameraOffsetSettings offset;
        public CameraDampingSettings damping;
        public CameraTimeScaleSettings timeScale;
        public CameraTiltSettings tilt;
        public CameraLockSettings lockTarget;
        public CameraPostProcessSettings postProcess;
        public CameraZoomSettings zoom;

        public float Duration
        {
            get
            {
                float duration = impulse.enabled ? Mathf.Max(0.01f, impulse.duration) : 0f;
                if (fov.enabled) duration = Mathf.Max(duration, fov.envelope.Duration);
                if (recoil.enabled) duration = Mathf.Max(duration, recoil.envelope.Duration);
                if (offset.enabled) duration = Mathf.Max(duration, offset.envelope.Duration);
                if (damping.enabled) duration = Mathf.Max(duration, damping.envelope.Duration);
                if (timeScale.enabled) duration = Mathf.Max(duration, timeScale.envelope.Duration);
                if (tilt.enabled) duration = Mathf.Max(duration, tilt.envelope.Duration);
                if (lockTarget.enabled) duration = Mathf.Max(duration, lockTarget.envelope.Duration);
                if (postProcess.enabled) duration = Mathf.Max(duration, postProcess.envelope.Duration);
                if (zoom.enabled) duration = Mathf.Max(duration, zoom.envelope.Duration);
                return Mathf.Max(0.01f, duration);
            }
        }
    }
}

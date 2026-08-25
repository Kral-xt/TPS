using UnityEngine;

namespace TPS.CameraSystem.Infrastructure
{
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "TPS/Camera/Camera Config")]
    public sealed class CameraConfig : ScriptableObject
    {


        [Header("跟随")]
        public Vector3 TargetOffset = new Vector3(0f, 1.55f, 0f);
        [Min(0.1f)] public float FollowDistance = 5f;
        public float FollowHeight = 0.25f;
        [Min(0.001f)] public float FollowSmoothTime = 0.045f;

        [Header("视角")]
        [Min(0f)] public float MouseSensitivityX = 2.8f;
        [Min(0f)] public float MouseSensitivityY = 2.2f;
        [Range(-89f, 89f)] public float MinPitch = -35f;
        [Range(-89f, 89f)] public float MaxPitch = 65f;
        public bool LockCursorOnStart = true;

        [Header("碰撞")]
        public LayerMask CollisionMask = ~0;
        [Min(0f)] public float CollisionRadius = 0.25f;
        [Min(0f)] public float CollisionPadding = 0.08f;

        [Header("视野")]
        [Range(10f, 120f)] public float NormalFov = 60f;
        [Range(10f, 120f)] public float SprintFov = 68f;
        [Min(0f)] public float FovLerpSpeed = 8f;
        [Range(10f, 90f)] public float AimFov = 42f;

        private void OnValidate()
        {
            MaxPitch = Mathf.Max(MinPitch, MaxPitch);
            SprintFov = Mathf.Max(NormalFov, SprintFov);
        }
    }
}

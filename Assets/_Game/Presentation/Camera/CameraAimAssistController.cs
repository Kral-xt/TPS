using TPS.Weapon.Application;
using TPS.Weapon.Infrastructure;
using UnityEngine;

namespace TPS.CameraSystem.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CameraAimAssistController : MonoBehaviour
    {
        private WeaponConfig config;
        private Transform ownerRoot;
        private WeaponAimAssistResolver targetResolver;

        public float Strength => config != null
            ? Mathf.Clamp01(config.AimAssistStrength)
            : 0f;

        public void Initialize(Transform playerRoot)
        {
            ownerRoot = playerRoot;
            ResolveConfigAndTargetResolver();
        }

        public bool TryGetAssistAngles(
            Camera gameCamera,
            out float targetYaw,
            out float targetPitch)
        {
            targetYaw = 0f;
            targetPitch = 0f;
            if (gameCamera == null || !ResolveConfigAndTargetResolver())
            {
                return false;
            }

            if (!targetResolver.TryGetTargetPoint(gameCamera, out Vector3 targetPoint))
            {
                return false;
            }

            Vector3 direction = targetPoint - gameCamera.transform.position;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            Vector3 targetEuler = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles;
            targetYaw = targetEuler.y;
            targetPitch = NormalizePitch(targetEuler.x);
            return true;
        }

        private bool ResolveConfigAndTargetResolver()
        {
            config ??= WeaponConfigProvider.Load();
            if (config == null || ownerRoot == null)
            {
                return false;
            }

            targetResolver ??= new WeaponAimAssistResolver(config, ownerRoot);
            return true;
        }

        private static float NormalizePitch(float rawPitch)
        {
            return rawPitch > 180f ? rawPitch - 360f : rawPitch;
        }
    }
}

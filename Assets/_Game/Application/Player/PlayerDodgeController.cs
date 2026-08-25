using System;
using TPS.Application.Abstractions;
using TPS.BulletTime.Application;
using TPS.Player.Domain;
using TPS.Player.Infrastructure;
using TPS.Player.Presentation;
using UnityEngine;

namespace TPS.Player.Application
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerDodgeController : MonoBehaviour
    {
        private readonly RaycastHit[] castHits = new RaycastHit[16];

        private PlayerConfig config;
        private CharacterController characterController;
        private TpsPrototypePlayerController playerController;
        private PlayerHealthController health;
        private PlayerAfterimageController afterimage;
        private BulletTimeController bulletTimeController;
        private float dodgeElapsed;
        private float dodgeDistance;
        private float previousDistance;
        private float cooldownUntil;
        private Vector3 dodgeDirection;
        private bool restorePlayerController;

        public PlayerDodgeState State { get; private set; } = PlayerDodgeState.Normal;
        public bool IsDodging => State == PlayerDodgeState.Dodging;
        public Vector3 DodgeDirection => dodgeDirection;
        public event Action<Vector3> DodgeStarted;
        public event Action DodgeFinished;

        private void Awake()
        {
            config = PlayerConfigProvider.Load();
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<TpsPrototypePlayerController>();
            health = GetComponent<PlayerHealthController>();
            afterimage = GetComponent<PlayerAfterimageController>();
        }

        private void Update()
        {
            if (!IsDodging)
            {
                return;
            }

            if (health != null && health.IsDead)
            {
                CancelDodge(true);
                return;
            }

            dodgeElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(dodgeElapsed / Mathf.Max(0.01f, config.DodgeDuration));
            float easedProgress = progress * progress * (3f - 2f * progress);
            float targetDistance = dodgeDistance * easedProgress;
            float step = targetDistance - previousDistance;
            previousDistance = targetDistance;
            if (step > 0f)
            {
                characterController.Move(dodgeDirection * step);
            }

            afterimage?.TickTrail();
            if (progress >= 1f)
            {
                FinishDodge();
            }
        }

        private void OnDisable()
        {
            CancelDodge(false);
        }

        public bool TryStartDodge(Vector3 direction)
        {
            if (health != null && health.IsDead)
            {
                return false;
            }

            if (State != PlayerDodgeState.Normal
                || Time.time < cooldownUntil
                || health == null
                || health.IsDead
                || characterController == null
                || !characterController.enabled
                || direction.sqrMagnitude < 0.0001f
                || (playerController != null
                    && (!playerController.enabled
                        || playerController.IsSliding
                        || playerController.IsCrouching
                        || playerController.IsAirborne
                        || playerController.IsTraversalBusy)))
            {
                return false;
            }

            dodgeDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
            dodgeDistance = CalculateSafeDistance(dodgeDirection, config.DodgeDistance);
            if (dodgeDistance <= 0.001f)
            {
                return false;
            }

            // 消耗常驻子弹时间能量
            BulletTimeController btController = ResolveBulletTimeController();
            if (btController != null && btController.DodgeCost > 0f)
            {
                if (!btController.TryConsumeNormalEnergy(btController.DodgeCost))
                {
                    return false;
                }
            }

            transform.rotation = Quaternion.LookRotation(dodgeDirection, Vector3.up);
            dodgeElapsed = 0f;
            previousDistance = 0f;
            cooldownUntil = Time.time + config.DodgeCooldown;
            State = PlayerDodgeState.Dodging;

            if (playerController != null)
            {
                restorePlayerController = playerController.enabled;
                playerController.enabled = false;
            }

            health.SetInvincible(config.DodgeInvincibleDuration);
            afterimage?.BeginTrail();
            GameAudio.Current?.PlayDodgeSound();
            DodgeStarted?.Invoke(dodgeDirection);
            return true;
        }

        private BulletTimeController ResolveBulletTimeController()
        {
            if (bulletTimeController == null)
            {
                bulletTimeController = BulletTimeController.Current;
            }

            return bulletTimeController;
        }

        public void SetDisabled(bool disabled)
        {
            if (disabled)
            {
                CancelDodge(true);
                State = PlayerDodgeState.Disabled;
            }
            else if (State == PlayerDodgeState.Disabled)
            {
                State = PlayerDodgeState.Normal;
            }
        }

        private float CalculateSafeDistance(Vector3 direction, float requestedDistance)
        {
            if (requestedDistance <= 0f)
            {
                return 0f;
            }

            Vector3 up = transform.up;
            Vector3 center = transform.TransformPoint(characterController.center);
            float scaleY = Mathf.Abs(transform.lossyScale.y);
            float radiusScale = Mathf.Max(
                Mathf.Abs(transform.lossyScale.x),
                Mathf.Abs(transform.lossyScale.z));
            float radius = characterController.radius * radiusScale;
            float height = Mathf.Max(characterController.height * scaleY, radius * 2f);
            float halfSegment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 top = center + up * halfSegment;
            Vector3 bottom = center - up * halfSegment;

            int hitCount = Physics.CapsuleCastNonAlloc(
                top,
                bottom,
                radius,
                direction,
                castHits,
                requestedDistance,
                config.DodgeCollisionMask,
                QueryTriggerInteraction.Ignore);

            float safeDistance = requestedDistance;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = castHits[i].collider;
                if (hitCollider == null
                    || hitCollider == characterController
                    || hitCollider.transform.IsChildOf(transform))
                {
                    continue;
                }

                safeDistance = Mathf.Min(
                    safeDistance,
                    Mathf.Max(0f, castHits[i].distance - config.DodgeCollisionSkin));
            }

            return safeDistance;
        }

        private void FinishDodge()
        {
            health?.ClearInvincibility();
            afterimage?.EndTrail();
            State = PlayerDodgeState.Normal;
            RestorePlayerController();
            DodgeFinished?.Invoke();
        }

        private void CancelDodge(bool disable)
        {
            if (State == PlayerDodgeState.Dodging)
            {
                health?.ClearInvincibility();
                afterimage?.EndTrail();
                RestorePlayerController();
                DodgeFinished?.Invoke();
            }

            State = disable ? PlayerDodgeState.Disabled : PlayerDodgeState.Normal;
        }

        private void RestorePlayerController()
        {
            if (playerController != null && restorePlayerController)
            {
                playerController.enabled = true;
            }

            restorePlayerController = false;
        }
    }
}

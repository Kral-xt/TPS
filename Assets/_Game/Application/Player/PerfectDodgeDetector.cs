using System.Collections.Generic;
using TPS.BulletTime.Application;
using TPS.Combat.Application;
using TPS.Combat.Domain;
using TPS.Combat.Presentation;
using TPS.Player.Infrastructure;
using UnityEngine;

namespace TPS.Player.Application
{
    [DisallowMultipleComponent]
    public sealed class PerfectDodgeDetector : MonoBehaviour
    {
        private readonly Dictionary<long, EnemyAttackStartedEvent> activeAttacks = new();
        private readonly HashSet<long> rewardedAttacks = new();
        private readonly List<long> expiredAttackIds = new();

        private PlayerConfig config;
        private PlayerDodgeController dodgeController;
        private CharacterController characterController;
        private bool projectileRewardedDuringCurrentDodge;

        private void Awake()
        {
            config = PlayerConfigProvider.Load();
            dodgeController = GetComponent<PlayerDodgeController>();
            characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            CombatRuntimeEvents.EnemyAttackStarted += OnEnemyAttackStarted;
            if (dodgeController != null)
            {
                dodgeController.DodgeStarted += OnDodgeStarted;
                dodgeController.DodgeFinished += OnDodgeFinished;
            }
        }

        private void OnDisable()
        {
            CombatRuntimeEvents.EnemyAttackStarted -= OnEnemyAttackStarted;
            if (dodgeController != null)
            {
                dodgeController.DodgeStarted -= OnDodgeStarted;
                dodgeController.DodgeFinished -= OnDodgeFinished;
            }

            activeAttacks.Clear();
            rewardedAttacks.Clear();
            projectileRewardedDuringCurrentDodge = false;
        }

        private void Update()
        {
            expiredAttackIds.Clear();
            foreach (KeyValuePair<long, EnemyAttackStartedEvent> pair in activeAttacks)
            {
                float expiry = pair.Value.AttackStartTime
                    + pair.Value.AttackActiveWindow
                    + config.PerfectDodgeWindow;
                if (Time.time > expiry)
                {
                    expiredAttackIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < expiredAttackIds.Count; i++)
            {
                long attackId = expiredAttackIds[i];
                activeAttacks.Remove(attackId);
                rewardedAttacks.Remove(attackId);
            }

            if (dodgeController != null && dodgeController.IsDodging)
            {
                TryRewardProjectile();
            }
        }

        private void OnEnemyAttackStarted(EnemyAttackStartedEvent attackEvent)
        {
            activeAttacks[attackEvent.AttackId] = attackEvent;
            if (dodgeController != null && dodgeController.IsDodging)
            {
                TryReward(attackEvent);
            }
        }

        private void OnDodgeStarted(Vector3 _)
        {
            projectileRewardedDuringCurrentDodge = false;
            foreach (KeyValuePair<long, EnemyAttackStartedEvent> pair in activeAttacks)
            {
                TryReward(pair.Value);
            }

            TryRewardProjectile();
        }

        private void OnDodgeFinished()
        {
            projectileRewardedDuringCurrentDodge = false;
        }

        private void TryReward(EnemyAttackStartedEvent attackEvent)
        {
            if (attackEvent.AttackId <= 0L
                || rewardedAttacks.Contains(attackEvent.AttackId)
                || !IsAttackEligible(attackEvent))
            {
                return;
            }

            rewardedAttacks.Add(attackEvent.AttackId);
            CombatFloatingTextManager.ShowMiss(transform);
            BulletTimeController.EnsureRuntimeInstance()
                .TriggerPerfectDodge();
        }

        private bool IsAttackEligible(EnemyAttackStartedEvent attackEvent)
        {
            float earliest = attackEvent.AttackStartTime - config.PerfectDodgeWindow;
            float latest = attackEvent.AttackStartTime
                + attackEvent.AttackActiveWindow
                + config.PerfectDodgeWindow;
            if (Time.time < earliest || Time.time > latest)
            {
                return false;
            }

            Vector3 toPlayer = transform.position - attackEvent.AttackPosition;
            toPlayer.y = 0f;
            float allowedRange = Mathf.Min(
                config.DodgeTriggerRange * config.PerfectDodgeRangeMultiplier,
                attackEvent.AttackRange * config.PerfectDodgeRangeMultiplier);
            if (toPlayer.sqrMagnitude > allowedRange * allowedRange)
            {
                return false;
            }

            if (toPlayer.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            float attackHalfAngle = Mathf.Acos(
                Mathf.Clamp(attackEvent.AttackFacingDot, -1f, 1f))
                * Mathf.Rad2Deg;
            float toleratedHalfAngle = Mathf.Min(
                180f,
                attackHalfAngle + config.PerfectDodgeAngleTolerance);
            float toleratedFacingDot = Mathf.Cos(toleratedHalfAngle * Mathf.Deg2Rad);
            return Vector3.Dot(attackEvent.AttackDirection, toPlayer.normalized)
                >= toleratedFacingDot;
        }

        private void TryRewardProjectile()
        {
            if (config == null)
            {
                return;
            }

            IReadOnlyList<IDodgeDetectableProjectile> projectiles =
                ProjectileDodgeRegistry.Projectiles;
            bool markedAnyProjectile = false;
            for (int i = 0; i < projectiles.Count; i++)
            {
                IDodgeDetectableProjectile projectile = projectiles[i];
                if (!IsProjectileEligible(projectile)
                    || !projectile.TryMarkDodgeRewarded())
                {
                    continue;
                }

                markedAnyProjectile = true;
            }

            if (markedAnyProjectile && !projectileRewardedDuringCurrentDodge)
            {
                projectileRewardedDuringCurrentDodge = true;
                CombatFloatingTextManager.ShowMiss(transform);
                BulletTimeController.EnsureRuntimeInstance()
                    .TriggerPerfectDodge();
            }
        }

        private bool IsProjectileEligible(IDodgeDetectableProjectile projectile)
        {
            if (projectile == null
                || projectile.AttackId <= 0L
                || projectile.ProjectileType != ProjectileType.EnemyBullet
                || !projectile.CanTriggerDodge
                || !projectile.IsDodgeCandidateActive)
            {
                return false;
            }

            Vector3 velocity = projectile.Velocity;
            float velocitySq = velocity.sqrMagnitude;
            if (velocitySq <= 0.0001f)
            {
                return false;
            }

            Vector3 playerCenter = characterController != null
                ? characterController.bounds.center
                : transform.position;
            Vector3 toPlayer = playerCenter - projectile.Position;
            float allowedRange = config.DodgeTriggerRange
                * config.PerfectDodgeRangeMultiplier;
            if (toPlayer.sqrMagnitude > allowedRange * allowedRange)
            {
                return false;
            }

            float approachDot = Vector3.Dot(toPlayer, velocity);
            if (approachDot <= 0f)
            {
                return false;
            }

            float timeToClosest = approachDot / velocitySq;
            if (timeToClosest > config.PerfectDodgeWindow)
            {
                return false;
            }

            Vector3 closestOffset = toPlayer - velocity * timeToClosest;
            float playerRadius = characterController != null
                ? characterController.radius * Mathf.Max(
                    Mathf.Abs(transform.lossyScale.x),
                    Mathf.Abs(transform.lossyScale.z))
                : 0.5f;
            float collisionRadius = playerRadius + projectile.HitRadius;
            return closestOffset.sqrMagnitude <= collisionRadius * collisionRadius;
        }
    }
}

using System.Collections.Generic;
using TPS.Combat.Domain;
using UnityEngine;

namespace TPS.Combat.Application
{
    public interface IDodgeDetectableProjectile
    {
        long AttackId { get; }
        ProjectileType ProjectileType { get; }
        Vector3 Position { get; }
        Vector3 Velocity { get; }
        float HitRadius { get; }
        bool CanTriggerDodge { get; }
        bool IsDodgeCandidateActive { get; }
        bool TryMarkDodgeRewarded();
    }

    public static class ProjectileDodgeRegistry
    {
        private static readonly List<IDodgeDetectableProjectile> projectiles = new();

        public static IReadOnlyList<IDodgeDetectableProjectile> Projectiles => projectiles;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            projectiles.Clear();
        }

        public static void Register(IDodgeDetectableProjectile projectile)
        {
            if (projectile != null && !projectiles.Contains(projectile))
            {
                projectiles.Add(projectile);
            }
        }

        public static void Unregister(IDodgeDetectableProjectile projectile)
        {
            if (projectile != null)
            {
                projectiles.Remove(projectile);
            }
        }
    }
}

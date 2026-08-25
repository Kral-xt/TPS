using System;

namespace TPS.Combat.Application
{
    public static class CombatPresentationEvents
    {
        public static event Action<bool> CrosshairTargetChanged;
        public static event Action EnemyHit;

        public static bool IsTargetingEnemy { get; private set; }

        public static void PublishCrosshairTargetChanged(bool isTargetingEnemy)
        {
            if (IsTargetingEnemy == isTargetingEnemy)
            {
                return;
            }

            IsTargetingEnemy = isTargetingEnemy;
            CrosshairTargetChanged?.Invoke(isTargetingEnemy);
        }

        public static void PublishEnemyHit()
        {
            EnemyHit?.Invoke();
        }
    }
}

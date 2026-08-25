using System;
using TPS.Combat.Domain;

namespace TPS.Combat.Application
{
    public readonly struct EnemyKilledEvent
    {
        public EnemyKilledEvent(
            object victim,
            DamageSourceKind source,
            object instigator,
            bool isHeadShot = false)
        {
            Victim = victim;
            Source = source;
            Instigator = instigator;
            IsHeadShot = isHeadShot;
        }

        public object Victim { get; }
        public DamageSourceKind Source { get; }
        public object Instigator { get; }
        public bool IsHeadShot { get; }
    }

    public static class CombatEventHub
    {
        public static event Action<EnemyKilledEvent> EnemyKilled;

        public static void PublishEnemyKilled(EnemyKilledEvent killedEvent)
        {
            EnemyKilled?.Invoke(killedEvent);
        }
    }
}

using System;
using System.Threading;
using TPS.Combat.Domain;
using UnityEngine;

namespace TPS.Combat.Application
{
    public readonly struct EnemyExperienceRewardedEvent
    {
        public EnemyExperienceRewardedEvent(
            object victim,
            DamageSourceKind source,
            object instigator,
            int amount)
        {
            Victim = victim;
            Source = source;
            Instigator = instigator;
            Amount = Mathf.Max(0, amount);
        }

        public object Victim { get; }
        public DamageSourceKind Source { get; }
        public object Instigator { get; }
        public int Amount { get; }
    }

    public readonly struct EnemyAttackStartedEvent
    {
        public EnemyAttackStartedEvent(
            long attackId,
            object attacker,
            Vector3 attackPosition,
            Vector3 attackDirection,
            float attackRange,
            float attackStartTime,
            float attackActiveWindow,
            float attackFacingDot)
        {
            AttackId = attackId;
            Attacker = attacker;
            AttackPosition = attackPosition;
            AttackDirection = attackDirection.sqrMagnitude > 0.0001f
                ? attackDirection.normalized
                : Vector3.forward;
            AttackRange = Mathf.Max(0f, attackRange);
            AttackStartTime = attackStartTime;
            AttackActiveWindow = Mathf.Max(0f, attackActiveWindow);
            AttackFacingDot = Mathf.Clamp(attackFacingDot, -1f, 1f);
        }

        public long AttackId { get; }
        public object Attacker { get; }
        public Vector3 AttackPosition { get; }
        public Vector3 AttackDirection { get; }
        public float AttackRange { get; }
        public float AttackStartTime { get; }
        public float AttackActiveWindow { get; }
        public float AttackFacingDot { get; }
    }

    public static class CombatRuntimeEvents
    {
        private static long nextAttackId;

        public static event Action<EnemyExperienceRewardedEvent> EnemyExperienceRewarded;
        public static event Action<EnemyAttackStartedEvent> EnemyAttackStarted;

        public static long CreateAttackId()
        {
            return Interlocked.Increment(ref nextAttackId);
        }

        public static void PublishEnemyExperienceRewarded(EnemyExperienceRewardedEvent rewardedEvent)
        {
            EnemyExperienceRewarded?.Invoke(rewardedEvent);
        }

        public static void PublishEnemyAttackStarted(EnemyAttackStartedEvent attackEvent)
        {
            EnemyAttackStarted?.Invoke(attackEvent);
        }
    }
}

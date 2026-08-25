using System;

namespace TPS.Player.Domain
{
    public readonly struct DamageResult
    {
        public DamageResult(float baseDamage, float finalDamage, bool isCritical)
        {
            BaseDamage = baseDamage;
            FinalDamage = finalDamage;
            IsCritical = isCritical;
        }

        public float BaseDamage { get; }
        public float FinalDamage { get; }
        public bool IsCritical { get; }
    }

    public static class PlayerCombatRules
    {
        public static DamageResult ResolveDamage(
            float baseDamage,
            float criticalRate,
            float criticalDamage,
            float randomValue)
        {
            float safeBaseDamage = Math.Max(0f, baseDamage);
            float safeCriticalRate = Math.Max(0f, Math.Min(1f, criticalRate));
            float safeCriticalDamage = Math.Max(0f, criticalDamage);
            bool isCritical = randomValue < safeCriticalRate;
            float finalDamage = isCritical
                ? safeBaseDamage * (1f + safeCriticalDamage)
                : safeBaseDamage;
            return new DamageResult(safeBaseDamage, finalDamage, isCritical);
        }
    }
}

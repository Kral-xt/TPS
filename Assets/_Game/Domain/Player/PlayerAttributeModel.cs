using System;

namespace TPS.Player.Domain
{
    public readonly struct ExperienceChangeResult
    {
        public ExperienceChangeResult(int previousLevel, int currentLevel, int currentExp, int requiredExp)
        {
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
            CurrentExp = currentExp;
            RequiredExp = requiredExp;
        }

        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public int CurrentExp { get; }
        public int RequiredExp { get; }
        public int LevelsGained => CurrentLevel - PreviousLevel;
    }

    public sealed class PlayerAttributeModel
    {
        private readonly float expGrowthRate;

        public PlayerAttributeModel(
            float maxHp,
            int initialLevel,
            int initialRequiredExp,
            float expGrowthRate,
            float criticalRate,
            float criticalDamage)
        {
            MaxHp = Math.Max(1f, maxHp);
            CurrentHp = MaxHp;
            Level = Math.Max(1, initialLevel);
            RequiredExp = Math.Max(1, initialRequiredExp);
            this.expGrowthRate = Math.Max(1f, expGrowthRate);
            CriticalRate = Clamp01(criticalRate);
            CriticalDamage = Math.Max(0f, criticalDamage);
        }

        public float MaxHp { get; private set; }
        public float CurrentHp { get; private set; }
        public int Level { get; private set; }
        public int CurrentExp { get; private set; }
        public int RequiredExp { get; private set; }
        public float CriticalRate { get; private set; }
        public float CriticalDamage { get; private set; }
        public bool IsDead => CurrentHp <= 0f;

        public float TakeDamage(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            float previous = CurrentHp;
            CurrentHp = Math.Max(0f, Math.Min(MaxHp, CurrentHp - amount));
            return previous - CurrentHp;
        }

        public float Heal(float amount)
        {
            if (amount <= 0f || IsDead)
            {
                return 0f;
            }

            float previous = CurrentHp;
            CurrentHp = Math.Max(0f, Math.Min(MaxHp, CurrentHp + amount));
            return CurrentHp - previous;
        }

        public void Kill()
        {
            CurrentHp = 0f;
        }


        public void SetMaxHp(float value, bool refill)
        {
            MaxHp = Math.Max(1f, value);
            CurrentHp = refill ? MaxHp : Math.Max(0f, Math.Min(MaxHp, CurrentHp));
        }

        public void SetCombatAttributes(float criticalRate, float criticalDamage)
        {
            CriticalRate = Clamp01(criticalRate);
            CriticalDamage = Math.Max(0f, criticalDamage);
        }

public ExperienceChangeResult AddExperience(int amount)
        {
            int previousLevel = Level;
            if (amount <= 0)
            {
                return new ExperienceChangeResult(previousLevel, Level, CurrentExp, RequiredExp);
            }

            CurrentExp += amount;
            while (CurrentExp >= RequiredExp)
            {
                CurrentExp -= RequiredExp;
                Level++;
                double nextRequiredExp = RequiredExp * (double)expGrowthRate;
                RequiredExp = Math.Max(1, (int)Math.Ceiling(nextRequiredExp - 0.0001d));
            }

            return new ExperienceChangeResult(previousLevel, Level, CurrentExp, RequiredExp);
        }

        private static float Clamp01(float value)
        {
            return Math.Max(0f, Math.Min(1f, value));
        }
    }
}

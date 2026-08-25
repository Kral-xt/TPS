namespace TPS.Player.Domain
{
    public readonly struct PlayerHpChangedEvent
    {
        public PlayerHpChangedEvent(float currentHp, float maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        public float CurrentHp { get; }
        public float MaxHp { get; }
    }

    public readonly struct PlayerDamagedEvent
    {
        public PlayerDamagedEvent(float damage, float currentHp, float maxHp)
        {
            Damage = damage;
            CurrentHp = currentHp;
            MaxHp = maxHp;
        }

        public float Damage { get; }
        public float CurrentHp { get; }
        public float MaxHp { get; }
    }

    public readonly struct PlayerExpChangedEvent
    {
        public PlayerExpChangedEvent(int level, int currentExp, int requiredExp)
        {
            Level = level;
            CurrentExp = currentExp;
            RequiredExp = requiredExp;
        }

        public int Level { get; }
        public int CurrentExp { get; }
        public int RequiredExp { get; }
    }

    public readonly struct PlayerLevelChangedEvent
    {
        public PlayerLevelChangedEvent(int previousLevel, int currentLevel)
        {
            PreviousLevel = previousLevel;
            CurrentLevel = currentLevel;
        }

        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
    }
}

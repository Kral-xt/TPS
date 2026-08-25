namespace TPS.Player.Domain
{
    public readonly struct PlayerDiedEvent
    {
    }

    public readonly struct PlayerLevelUpEvent
    {
        public PlayerLevelUpEvent(int level)
        {
            Level = level;
        }

        public int Level { get; }
    }
}

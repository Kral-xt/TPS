using System;
using TPS.Player.Domain;
using TPS.Player.Infrastructure;
using UnityEngine;

namespace TPS.Player.Application
{
    [DisallowMultipleComponent]
    public sealed class PlayerAttributeController : MonoBehaviour
    {
        private PlayerAttributeModel model;

        public PlayerConfig Config { get; private set; }
        internal PlayerAttributeModel Model => model;
        public float MaxHp => model != null ? model.MaxHp : 0f;
        public float CurrentHp => model != null ? model.CurrentHp : 0f;
        public int Level => model != null ? model.Level : 1;
        public int CurrentExp => model != null ? model.CurrentExp : 0;
        public int RequiredExp => model != null ? model.RequiredExp : 1;
        public float CriticalRate => model != null ? model.CriticalRate : 0f;
        public float CriticalDamage => model != null ? model.CriticalDamage : 0f;
        public bool IsDead => model != null && model.IsDead;

        public event Action<PlayerHpChangedEvent> HpChanged;
        public event Action<PlayerDamagedEvent> Damaged;
        public event Action<PlayerExpChangedEvent> ExpChanged;
        public event Action<PlayerLevelChangedEvent> LevelChanged;
        public event Action<PlayerLevelUpEvent> LevelUp;
        public event Action<PlayerDiedEvent> Died;

        private void Awake()
        {
            Config = PlayerConfigProvider.Load();
            model = new PlayerAttributeModel(
                Config.MaxHp,
                Config.InitialLevel,
                Config.InitialRequiredExp,
                Config.ExpGrowthRate,
                Config.CriticalRate,
                Config.CriticalDamage);
        }

        public void PublishCurrentState()
        {
            PublishHpChanged();
            PublishExpChanged();
        }

        internal void PublishHpChanged()
        {
            HpChanged?.Invoke(new PlayerHpChangedEvent(CurrentHp, MaxHp));
        }

        internal void PublishDamaged(float damage)
        {
            Damaged?.Invoke(new PlayerDamagedEvent(damage, CurrentHp, MaxHp));
        }

        internal void PublishExpChanged()
        {
            ExpChanged?.Invoke(new PlayerExpChangedEvent(Level, CurrentExp, RequiredExp));
        }

        internal void PublishLevelChanged(int previousLevel)
        {
            LevelChanged?.Invoke(new PlayerLevelChangedEvent(previousLevel, Level));
        }

        internal void PublishLevelUp(int level)
        {
            LevelUp?.Invoke(new PlayerLevelUpEvent(level));
        }

        internal void PublishDied()
        {
            Died?.Invoke(new PlayerDiedEvent());
        }
    }
}

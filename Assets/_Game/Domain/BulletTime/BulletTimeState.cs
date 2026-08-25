namespace TPS.BulletTime.Domain
{
    public enum BulletTimeState
    {
        Inactive,
        Entering,
        Active,
        Exiting,
        Disabled
    }

    public enum BulletTimeEnergyChangeReason
    {
        Consume,
        NaturalRecovery,
        KillRecovery,
        PerfectDodgeRecovery,
        Reset
    }

    public enum BulletTimeSource
    {
        None,
        Normal,
        PerfectDodge
    }

    public sealed class BulletTimeEnergyModel
    {
        public BulletTimeEnergyModel(float maxEnergy)
        {
            MaxEnergy = maxEnergy > 0f ? maxEnergy : 100f;
            CurrentEnergy = MaxEnergy;
        }

        public float MaxEnergy { get; }
        public float CurrentEnergy { get; private set; }

        public bool Consume(float amount)
        {
            return SetEnergy(CurrentEnergy - amount);
        }

        public bool Recover(float amount)
        {
            return SetEnergy(CurrentEnergy + amount);
        }

        public bool Reset()
        {
            return SetEnergy(MaxEnergy);
        }

        private bool SetEnergy(float value)
        {
            float clamped = value < 0f ? 0f : value > MaxEnergy ? MaxEnergy : value;
            if (System.Math.Abs(CurrentEnergy - clamped) < 0.0001f)
            {
                return false;
            }

            CurrentEnergy = clamped;
            return true;
        }
    }

    public sealed class PerfectDodgeBulletTimeModel
    {
        public PerfectDodgeBulletTimeModel(float maxEnergy)
        {
            MaxEnergy = maxEnergy > 0f ? maxEnergy : 30f;
            CurrentEnergy = 0f;
        }

        public float MaxEnergy { get; }
        public float CurrentEnergy { get; private set; }

        public void Reset(float energy)
        {
            float clamped = energy < 0f ? 0f : energy > MaxEnergy ? MaxEnergy : energy;
            CurrentEnergy = clamped;
        }

        public bool Consume(float amount)
        {
            float clamped = CurrentEnergy - amount;
            if (clamped < 0f)
            {
                clamped = 0f;
            }

            if (System.Math.Abs(CurrentEnergy - clamped) < 0.0001f)
            {
                return false;
            }

            CurrentEnergy = clamped;
            return true;
        }
    }

}

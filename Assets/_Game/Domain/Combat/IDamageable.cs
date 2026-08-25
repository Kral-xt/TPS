namespace TPS.Combat.Domain
{
    public enum DamageSourceKind
    {
        Unknown,
        Player,
        Environment,
        Other
    }

    public readonly struct DamageInfo
    {
        public DamageInfo(
            float amount,
            DamageSourceKind source,
            object instigator,
            bool isCritical = false,
            HitPartType hitPart = HitPartType.Body)
        {
            Amount = amount;
            Source = source;
            Instigator = instigator;
            IsCritical = isCritical;
            HitPart = hitPart;
        }

        public float Amount { get; }
        public DamageSourceKind Source { get; }
        public object Instigator { get; }
        public bool IsCritical { get; }
        public HitPartType HitPart { get; }
    }

    public interface IDamageable
    {
        bool IsDead { get; }
        void ApplyDamage(float amount);
    }

    public interface IAttributedDamageable : IDamageable
    {
        void ApplyDamage(DamageInfo damageInfo);
    }
}

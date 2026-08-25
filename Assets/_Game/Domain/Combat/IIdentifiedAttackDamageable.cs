namespace TPS.Combat.Domain
{
    public interface IIdentifiedAttackDamageable : IAttributedDamageable
    {
        void ApplyDamage(DamageInfo damageInfo, long attackId);
    }
}

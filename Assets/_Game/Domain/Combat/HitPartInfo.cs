namespace TPS.Combat.Domain
{
    public enum HitPartType
    {
        Body,
        HeadShot
    }

    public readonly struct HitPartInfo
    {
        public HitPartInfo(HitPartType partType, float bonusCriticalChance)
        {
            PartType = partType;
            BonusCriticalChance = bonusCriticalChance;
        }

        public HitPartType PartType { get; }
        public float BonusCriticalChance { get; }
    }

    public interface IHitPartResolver
    {
        bool TryResolveHitPart(object hitObject, out HitPartInfo hitPart);
    }
}
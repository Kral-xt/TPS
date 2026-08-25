using TPS.Infrastructure.Config;

namespace TPS.Weapon.Infrastructure
{
    public static class WeaponConfigProvider
    {
        public static WeaponConfig Load()
        {
            return GameConfigManager.Resolve()?.WeaponConfig;
        }
    }
}

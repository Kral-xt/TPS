using TPS.Player.Domain;
using UnityEngine;

namespace TPS.Player.Application
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatAttributeController : MonoBehaviour
    {
        private PlayerAttributeController attributes;

        private void Awake()
        {
            attributes = GetComponent<PlayerAttributeController>();
        }

        public DamageResult CalculateDamage(float baseDamage)
        {
            return CalculateDamage(baseDamage, 0f);
        }

        public DamageResult CalculateDamage(float baseDamage, float bonusCriticalRate)
        {
            if (attributes == null)
            {
                return PlayerCombatRules.ResolveDamage(
                    baseDamage,
                    Mathf.Max(0f, bonusCriticalRate),
                    0f,
                    Random.value);
            }

            return PlayerCombatRules.ResolveDamage(
                baseDamage,
                attributes.CriticalRate + Mathf.Max(0f, bonusCriticalRate),
                attributes.CriticalDamage,
                Random.value);
        }
    }
}

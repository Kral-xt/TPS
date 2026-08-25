using TPS.Combat.Application;
using TPS.Combat.Domain;
using TPS.Player.Domain;
using UnityEngine;

namespace TPS.Player.Application
{
    [DisallowMultipleComponent]
    public sealed class PlayerLevelController : MonoBehaviour
    {
        private PlayerAttributeController attributes;

        private void Awake()
        {
            attributes = GetComponent<PlayerAttributeController>();
        }

        private void OnEnable()
        {
            CombatRuntimeEvents.EnemyExperienceRewarded += OnEnemyExperienceRewarded;
        }

        private void OnDisable()
        {
            CombatRuntimeEvents.EnemyExperienceRewarded -= OnEnemyExperienceRewarded;
        }

        public void AddExperience(int amount)
        {
            if (attributes == null || amount <= 0)
            {
                return;
            }

            ExperienceChangeResult result = attributes.Model.AddExperience(amount);
            for (int level = result.PreviousLevel + 1; level <= result.CurrentLevel; level++)
            {
                attributes.PublishLevelUp(level);
            }

            if (result.LevelsGained > 0)
            {
                attributes.PublishLevelChanged(result.PreviousLevel);
            }

            attributes.PublishExpChanged();
        }

        private void OnEnemyExperienceRewarded(EnemyExperienceRewardedEvent rewardedEvent)
        {
            if (rewardedEvent.Source != DamageSourceKind.Player
                || rewardedEvent.Amount <= 0
                || !BelongsToPlayer(rewardedEvent.Instigator))
            {
                return;
            }

            AddExperience(rewardedEvent.Amount);
        }

        private bool BelongsToPlayer(object instigator)
        {
            Transform instigatorTransform = instigator switch
            {
                Component component => component.transform,
                GameObject gameObject => gameObject.transform,
                _ => null
            };

            return instigatorTransform != null
                && (instigatorTransform == transform
                    || instigatorTransform.IsChildOf(transform)
                    || transform.IsChildOf(instigatorTransform));
        }
    }
}

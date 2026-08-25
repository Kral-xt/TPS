using System;
using TPS.BulletTime.Application;
using TPS.Combat.Domain;
using TPS.Player.Presentation;
using UnityEngine;

namespace TPS.Player.Application
{
    public readonly struct PlayerDamageAvoidedEvent
    {
        public PlayerDamageAvoidedEvent(DamageInfo damageInfo, long attackId)
        {
            DamageInfo = damageInfo;
            AttackId = attackId;
        }

        public DamageInfo DamageInfo { get; }
        public long AttackId { get; }
    }

    [DisallowMultipleComponent]
    public sealed class PlayerHealthController : MonoBehaviour, IIdentifiedAttackDamageable
    {
        private PlayerAttributeController attributes;
        private float invincibleUntil;
        private bool deathPublished;
        private PlayerAnimatorPresenter animatorPresenter;

        public bool IsDead => attributes != null && attributes.IsDead;
        public bool IsInvincible => Time.time < invincibleUntil;
        public event Action<PlayerDamageAvoidedEvent> DamageAvoided;

        private void Awake()
        {
            attributes = GetComponent<PlayerAttributeController>();
            TpsPrototypePlayerController playerController = GetComponent<TpsPrototypePlayerController>();
            if (playerController != null)
            {
                animatorPresenter = playerController.AnimatorPresenter;
            }
        }

        private void OnDisable()
        {
            ClearInvincibility();
        }

        public void ApplyDamage(float amount)
        {
            ApplyDamage(new DamageInfo(amount, DamageSourceKind.Unknown, null), 0L);
        }

        public void ApplyDamage(DamageInfo damageInfo)
        {
            ApplyDamage(damageInfo, 0L);
        }

        public void ApplyDamage(DamageInfo damageInfo, long attackId)
        {
            if (attributes == null || IsDead || damageInfo.Amount <= 0f)
            {
                return;
            }

            if (IsInvincible)
            {
                DamageAvoided?.Invoke(new PlayerDamageAvoidedEvent(damageInfo, attackId));
                return;
            }

            float appliedDamage = attributes.Model.TakeDamage(damageInfo.Amount);
            if (appliedDamage <= 0f)
            {
                return;
            }

            attributes.PublishDamaged(appliedDamage);
            attributes.PublishHpChanged();
            if (attributes.IsDead)
            {
                Die();
            }
            else
            {
                animatorPresenter?.PlayHitAnimation();
            }
        }

        public void TakeDamage(float damage)
        {
            ApplyDamage(damage);
        }

        public void Heal(float amount)
        {
            if (attributes == null || IsDead)
            {
                return;
            }

            if (attributes.Model.Heal(amount) > 0f)
            {
                attributes.PublishHpChanged();
            }
        }

        public void Die()
        {
            if (attributes == null || deathPublished)
            {
                return;
            }

            deathPublished = true;
            attributes.Model.Kill();
            attributes.PublishHpChanged();
            attributes.PublishDied();
            ClearInvincibility();
            BulletTimeController.Current?.SetDisabled(true);
            animatorPresenter?.PlayDeathAnimation();
        }

        public void SetInvincible(float duration)
        {
            if (duration <= 0f || IsDead)
            {
                return;
            }

            invincibleUntil = Mathf.Max(invincibleUntil, Time.time + duration);
        }

        public void ClearInvincibility()
        {
            invincibleUntil = float.NegativeInfinity;
        }
    }
}


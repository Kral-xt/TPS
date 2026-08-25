using TPS.Application.Abstractions;
using TPS.BulletTime.Application;
using TPS.BulletTime.Domain;
using TPS.BulletTime.Infrastructure;
using UnityEngine;

namespace TPS.BulletTime.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BulletTimeAudioController : MonoBehaviour
    {
        private BulletTimeController controller;
        private BulletTimeConfig config;
        private IGameAudioService audioService;
        private bool lowEnergyWarningPlayed;

        private void Awake()
        {
            controller = GetComponent<BulletTimeController>();
            config = controller != null ? controller.Config : null;
            audioService = GameAudio.Current;
        }

        private void OnEnable()
        {
            if (controller == null)
            {
                return;
            }

            controller.StateChanged += OnStateChanged;
            controller.SourceChanged += OnSourceChanged;
            controller.EnergyChanged += OnEnergyChanged;
            controller.ActivationRejected += OnActivationRejected;
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                controller.StateChanged -= OnStateChanged;
                controller.SourceChanged -= OnSourceChanged;
                controller.EnergyChanged -= OnEnergyChanged;
                controller.ActivationRejected -= OnActivationRejected;
            }

            audioService?.PauseBulletTimeBGM();
            audioService?.SetBulletTimeEffect(0f);
        }

        private void Update()
        {
            audioService ??= GameAudio.Current;
            if (controller != null)
            {
                audioService?.SetBulletTimeEffect(controller.EffectWeight);
            }
        }

        private void OnStateChanged(BulletTimeState state)
        {
            if (state == BulletTimeState.Entering)
            {
                ApplyBgmForSource(controller.CurrentSource);
                lowEnergyWarningPlayed = false;
            }
            else if (state == BulletTimeState.Exiting)
            {
                audioService?.PauseBulletTimeBGM();
                audioService?.PlayBulletTimeExitSound();
            }
            else if (state == BulletTimeState.Inactive)
            {
                audioService?.PauseBulletTimeBGM();
                lowEnergyWarningPlayed = false;
            }
            else if (state == BulletTimeState.Disabled)
            {
                audioService?.PauseBulletTimeBGM();
            }
        }

        private void OnSourceChanged(BulletTimeSource source)
        {
            if (controller == null
                || (controller.State != BulletTimeState.Entering
                    && controller.State != BulletTimeState.Active))
            {
                return;
            }

            if (source == BulletTimeSource.Normal)
            {
                audioService?.PlayBulletTimeBGM();
            }
            else
            {
                audioService?.PauseBulletTimeBGM();
            }
        }

        private void ApplyBgmForSource(BulletTimeSource source)
        {
            if (source == BulletTimeSource.Normal)
            {
                audioService?.PlayBulletTimeBGM();
            }
        }

        private void OnEnergyChanged(
            float currentEnergy,
            float maxEnergy,
            BulletTimeEnergyChangeReason reason)
        {
            if (config == null)
            {
                return;
            }

            if (reason != BulletTimeEnergyChangeReason.Consume)
            {
                if (currentEnergy > config.MinimumActivationEnergy)
                {
                    lowEnergyWarningPlayed = false;
                }
                return;
            }

            if (!lowEnergyWarningPlayed && currentEnergy <= config.MinimumActivationEnergy)
            {
                lowEnergyWarningPlayed = true;
                audioService?.PlayBulletTimeLowEnergySound();
            }
        }

        private void OnActivationRejected()
        {
            audioService?.PlayBulletTimeActivationRejectedSound();
        }
    }
}

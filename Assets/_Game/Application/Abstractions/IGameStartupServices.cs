using System;
using System.Collections;

namespace TPS.Application.Abstractions
{
    public interface IGameAudioService
    {
        void PlayMenuBGM();
        void StopMenuBGM();
        void PlayGamingBGM();
        void StopGamingBGM();
        void PlayBulletTimeBGM();
        void PauseBulletTimeBGM();
        void StopBulletTimeBGM();
        void PlayBulletTimeExitSound();
        void PlayBulletTimeLowEnergySound();
        void PlayBulletTimeActivationRejectedSound();
        void PlayDodgeSound();
        void PlayWeaponShotSound();
        void PlayEnemyBrokenSound();

        void PlayKillFeedback(int killCount, bool isHeadShot);
        void PlayEnemyDownSound();
        void PlayUIClickSound();
        void SetBulletTimeEffect(float weight);
    }

    public static class GameAudio
    {
        public static IGameAudioService Current { get; private set; }

        public static void Register(IGameAudioService service)
        {
            Current = service;
        }

        public static void Unregister(IGameAudioService service)
        {
            if (ReferenceEquals(Current, service))
            {
                Current = null;
            }
        }

        public static void Reset()
        {
            Current = null;
        }
    }

    public interface ISceneLoadService
    {
        IEnumerator LoadSceneAsync(
            string sceneName,
            Action<float> onProgress,
            Action<bool> onCompleted);
    }
}

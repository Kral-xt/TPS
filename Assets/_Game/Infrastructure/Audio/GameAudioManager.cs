using System.Collections.Generic;
using TPS.Application.Abstractions;
using TPS.Audio.Infrastructure;
using TPS.Infrastructure.Config;
using UnityEngine;

namespace TPS.Startup.Infrastructure
{
    [DisallowMultipleComponent]
    public sealed class GameAudioManager : MonoBehaviour, IGameAudioService
    {
        private enum BgmKind
        {
            None,
            Menu,
            Gaming
        }

        private static GameAudioManager instance;

        [Header("统一音频配置")]
        [SerializeField] private AudioConfig audioConfig;
        [SerializeField] private KillAudioConfig killAudioConfig;

        private AudioSource bgmSource;
        private AudioSource bulletTimeBgmSource;
        private AudioSource sfxSource;
        private AudioSource uiSource;
        private AudioListener menuAudioListener;
        private BgmKind currentBgm;
        private bool hasBulletTimeBgm;
        private bool primaryBgmPausedForBulletTime;
        private float menuVolumeOverride = -1f;
        private float gamingVolumeOverride = -1f;
        private float lastPitch = -1f;

        public static GameAudioManager EnsureRuntimeInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<GameAudioManager>();
            if (instance != null)
            {
                DontDestroyOnLoad(instance.gameObject);
                return instance;
            }

            GameObject managerObject = new GameObject("GameAudioManager");
            DontDestroyOnLoad(managerObject);
            instance = managerObject.AddComponent<GameAudioManager>();
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            ResolveAudioConfig();

            bgmSource = CreateSource("BGM AudioSource", true, 64);
            bulletTimeBgmSource = CreateSource("BulletTime BGM AudioSource", true, 64);
            sfxSource = CreateSource("SFX AudioSource", false, 128);
            uiSource = CreateSource("UI AudioSource", false, 96);
            menuAudioListener = gameObject.AddComponent<AudioListener>();
            menuAudioListener.enabled = false;

            WarmUpConfiguredAudio();
            GameAudio.Register(this);
        }

        public void PlayMenuBGM()
        {
            PauseBulletTimeForPrimaryBgmSwitch();
            if (menuAudioListener != null)
            {
                menuAudioListener.enabled = true;
            }

            PlayBgm(audioConfig.MenuBGM, BgmKind.Menu, menuVolumeOverride);
        }

        public void StopMenuBGM()
        {
            if (currentBgm == BgmKind.Menu)
            {
                StopBgm();
            }

            if (menuAudioListener != null)
            {
                menuAudioListener.enabled = false;
            }
        }

        public void PlayGamingBGM()
        {
            StopMenuBGM();
            PauseBulletTimeForPrimaryBgmSwitch();
            PlayBgm(audioConfig.GamingBGM, BgmKind.Gaming, gamingVolumeOverride);
        }

        public void StopGamingBGM()
        {
            if (currentBgm == BgmKind.Gaming)
            {
                StopBgm();
            }
        }

        public void PlayBulletTimeBGM()
        {
            AudioClipConfig bulletTimeBgm = audioConfig.BulletTimeBGM;
            if (bulletTimeBgmSource == null || bulletTimeBgm?.clip == null)
            {
                return;
            }

            if (!primaryBgmPausedForBulletTime
                && currentBgm != BgmKind.None
                && bgmSource != null
                && bgmSource.isPlaying)
            {
                bgmSource.Pause();
                primaryBgmPausedForBulletTime = true;
            }

            if (!hasBulletTimeBgm || bulletTimeBgmSource.clip != bulletTimeBgm.clip)
            {
                bulletTimeBgmSource.clip = bulletTimeBgm.clip;
                bulletTimeBgmSource.volume = bulletTimeBgm.volume;
                bulletTimeBgmSource.Play();
                hasBulletTimeBgm = true;
                return;
            }

            bulletTimeBgmSource.volume = bulletTimeBgm.volume;
            if (!bulletTimeBgmSource.isPlaying)
            {
                bulletTimeBgmSource.UnPause();
            }
        }

        public void PauseBulletTimeBGM()
        {
            if (bulletTimeBgmSource != null && hasBulletTimeBgm)
            {
                bulletTimeBgmSource.Pause();
            }

            if (primaryBgmPausedForBulletTime
                && currentBgm != BgmKind.None
                && bgmSource != null
                && bgmSource.clip != null)
            {
                bgmSource.UnPause();
            }

            primaryBgmPausedForBulletTime = false;
        }

        public void StopBulletTimeBGM()
        {
            PauseBulletTimeBGM();
        }

        public void PlayBulletTimeExitSound()
        {
            PlaySfx(audioConfig.BulletTimeExit);
        }

        public void PlayBulletTimeLowEnergySound()
        {
            PlaySfx(audioConfig.BulletTimeLowEnergy);
        }

        public void PlayBulletTimeActivationRejectedSound()
        {
            PlaySfx(audioConfig.BulletTimeActivationRejected);
        }

        public void PlayDodgeSound()
        {
            PlaySfx(audioConfig.DodgeSound);
        }

        public void PlayWeaponShotSound()
        {
            PlaySfx(audioConfig.WeaponShotSound);
        }

        public void PlayEnemyBrokenSound()
        {
            PlaySfx(audioConfig.EnemyBrokenSound);
        }

        public void PlayEnemyDownSound()
        {
            PlaySfx(audioConfig.EnemyDownSound);
        }

        public void PlayKillFeedback(int killCount, bool isHeadShot)
        {
            KillAudioConfig resolvedConfig = ResolveKillAudioConfig();
            if (resolvedConfig == null)
            {
                Debug.LogWarning("[GameAudioManager] 未找到 KillAudioConfig，无法播放击杀音效。", this);
                return;
            }

            AudioClip clip = resolvedConfig.GetClip(killCount, isHeadShot);
            if (clip == null)
            {
                Debug.LogWarning(
                    $"[GameAudioManager] 击杀音效未配置：killCount={killCount}, headShot={isHeadShot}。",
                    this);
                return;
            }

            PlayOneShot(sfxSource, clip);
        }



        public void PlayUIClickSound()
        {
            PlayOneShot(uiSource, audioConfig.UIClickSound);
        }

        public void SetBulletTimeEffect(float weight)
        {
            if (audioConfig.AudioMixer == null)
            {
                return;
            }

            float pitch = Mathf.Lerp(
                1f,
                audioConfig.BulletTimePitch,
                Mathf.Clamp01(weight));
            if (Mathf.Abs(lastPitch - pitch) <= 0.005f)
            {
                return;
            }

            audioConfig.AudioMixer.SetFloat(audioConfig.PitchParameter, pitch);
            lastPitch = pitch;
        }

        public void SetMenuVolume(float volume)
        {
            menuVolumeOverride = Mathf.Clamp01(volume);
            if (currentBgm == BgmKind.Menu && bgmSource != null)
            {
                bgmSource.volume = menuVolumeOverride;
            }
        }

        public void SetGamingVolume(float volume)
        {
            gamingVolumeOverride = Mathf.Clamp01(volume);
            if (currentBgm == BgmKind.Gaming && bgmSource != null)
            {
                bgmSource.volume = gamingVolumeOverride;
            }
        }

        private void ResolveAudioConfig()
        {
            GameConfigManager configManager = GameConfigManager.Resolve();
            audioConfig ??= configManager?.AudioConfig;
            killAudioConfig ??= configManager?.KillAudioConfig;
            if (audioConfig != null)
            {
                return;
            }

            audioConfig = ScriptableObject.CreateInstance<AudioConfig>();
            audioConfig.hideFlags = HideFlags.DontSave;
            Debug.LogError("[GameAudioManager] 未绑定 AudioConfig，当前音频将保持静默。", this);
        }

        private KillAudioConfig ResolveKillAudioConfig()
        {
            if (killAudioConfig != null)
            {
                return killAudioConfig;
            }

            killAudioConfig = GameConfigManager.Resolve()?.KillAudioConfig;
            return killAudioConfig;
        }



        private AudioSource CreateSource(string sourceName, bool loop, int priority)
        {
            GameObject sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;

            source.volume = 1f;
            source.mute = false;
            source.spatialBlend = 0f;
            source.ignoreListenerPause = true;
            source.priority = priority;
            return source;
        }

        private void PlayBgm(
            AudioClipConfig clipConfig,
            BgmKind kind,
            float volumeOverride = -1f)
        {
            if (bgmSource == null || clipConfig?.clip == null)
            {
                return;
            }

            if (currentBgm == kind
                && bgmSource.clip == clipConfig.clip
                && bgmSource.isPlaying)
            {
                return;
            }

            bgmSource.Stop();
            bgmSource.clip = clipConfig.clip;
            bgmSource.volume = volumeOverride >= 0f
                ? Mathf.Clamp01(volumeOverride)
                : clipConfig.volume;
            bgmSource.Play();
            currentBgm = kind;
        }

        private void StopBgm()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                bgmSource.clip = null;
            }

            currentBgm = BgmKind.None;
            primaryBgmPausedForBulletTime = false;
        }

        private void PauseBulletTimeForPrimaryBgmSwitch()
        {
            if (bulletTimeBgmSource != null && bulletTimeBgmSource.isPlaying)
            {
                bulletTimeBgmSource.Pause();
            }

            primaryBgmPausedForBulletTime = false;
        }

        private void PlaySfx(AudioClipConfig clipConfig)
        {
            PlayOneShot(sfxSource, clipConfig);
        }

        private static void PlayOneShot(AudioSource source, AudioClipConfig clipConfig)
        {
            if (source == null || clipConfig?.clip == null)
            {
                return;
            }

            source.PlayOneShot(clipConfig.clip, clipConfig.volume);
        }

        private static void PlayOneShot(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null)
            {
                return;
            }

            source.PlayOneShot(clip);
        }


        private void WarmUpConfiguredAudio()
        {
            WarmUpClip(audioConfig.MenuBGM);
            WarmUpClip(audioConfig.GamingBGM);
            WarmUpClip(audioConfig.BulletTimeBGM);
            WarmUpClip(audioConfig.BulletTimeExit);
            WarmUpClip(audioConfig.BulletTimeLowEnergy);
            WarmUpClip(audioConfig.BulletTimeActivationRejected);
            WarmUpClip(audioConfig.DodgeSound);
            WarmUpClip(audioConfig.WeaponShotSound);
            WarmUpClip(audioConfig.EnemyBrokenSound);
            WarmUpClip(audioConfig.EnemyDownSound);
            WarmUpClip(audioConfig.UIClickSound);

            if (killAudioConfig == null)
            {
                return;
            }

            IReadOnlyList<AudioClip> killClips = killAudioConfig.KillClips;
            for (int i = 0; i < killClips.Count; i++)
            {
                WarmUpClip(killClips[i]);
            }

            WarmUpClip(killAudioConfig.HeadShotClip);
        }


        private void WarmUpClip(AudioClipConfig clipConfig)
        {
            WarmUpClip(clipConfig?.clip);
        }


        private void WarmUpClip(AudioClip clip)
        {
            if (clip == null
                || clip.loadState == AudioDataLoadState.Loaded
                || clip.loadState == AudioDataLoadState.Loading)
            {
                return;
            }

            if (!clip.LoadAudioData())
            {
                Debug.LogWarning($"[GameAudioManager] 音频预热请求失败：{clip.name}", this);
            }
        }


        private void OnDestroy()
        {
            if (instance != this)
            {
                return;
            }

            SetBulletTimeEffect(0f);
            GameAudio.Unregister(this);
            instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeState()
        {
            instance = null;
            GameAudio.Reset();
        }
    }
}

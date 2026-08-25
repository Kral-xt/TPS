using System;
using UnityEngine;
using UnityEngine.Audio;

namespace TPS.Audio.Infrastructure
{
    [Serializable]
    public sealed class AudioClipConfig
    {
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }

    [CreateAssetMenu(fileName = "AudioConfig", menuName = "TPS/Audio/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        [Header("背景音乐")]
        [SerializeField] private AudioClipConfig menuBGM = new AudioClipConfig();
        [SerializeField] private AudioClipConfig gamingBGM = new AudioClipConfig();
        [SerializeField] private AudioClipConfig bulletTimeBGM = new AudioClipConfig();

        [Header("子弹时间音效")]
        [SerializeField] private AudioClipConfig bulletTimeExit = new AudioClipConfig();
        [SerializeField] private AudioClipConfig bulletTimeLowEnergy = new AudioClipConfig();
        [SerializeField] private AudioClipConfig bulletTimeActivationRejected = new AudioClipConfig();

        [Header("玩家与武器音效")]
        [SerializeField] private AudioClipConfig dodgeSound = new AudioClipConfig();
        [SerializeField] private AudioClipConfig weaponShotSound = new AudioClipConfig();

        [Header("敌人音效")]
        [SerializeField] private AudioClipConfig enemyBrokenSound = new AudioClipConfig();
        [SerializeField] private AudioClipConfig enemyDownSound = new AudioClipConfig();

        [Header("UI 音效")]
        [SerializeField] private AudioClipConfig uiClickSound = new AudioClipConfig();

        [Header("混音")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private string pitchParameter = "Pitch";
        [SerializeField, Range(0.01f, 1f)] private float bulletTimePitch = 0.45f;

        public AudioClipConfig MenuBGM => menuBGM;
        public AudioClipConfig GamingBGM => gamingBGM;
        public AudioClipConfig BulletTimeBGM => bulletTimeBGM;
        public AudioClipConfig BulletTimeExit => bulletTimeExit;
        public AudioClipConfig BulletTimeLowEnergy => bulletTimeLowEnergy;
        public AudioClipConfig BulletTimeActivationRejected => bulletTimeActivationRejected;
        public AudioClipConfig DodgeSound => dodgeSound;
        public AudioClipConfig WeaponShotSound => weaponShotSound;
        public AudioClipConfig EnemyBrokenSound => enemyBrokenSound;
        public AudioClipConfig EnemyDownSound => enemyDownSound;
        public AudioClipConfig UIClickSound => uiClickSound;
        public AudioMixer AudioMixer => audioMixer;
        public string PitchParameter => pitchParameter;
        public float BulletTimePitch => bulletTimePitch;
    }
}

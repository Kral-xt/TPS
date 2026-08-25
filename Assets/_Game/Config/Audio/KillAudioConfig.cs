using System.Collections.Generic;
using UnityEngine;

namespace TPS.Audio.Infrastructure
{
    [CreateAssetMenu(fileName = "KillAudioConfig", menuName = "TPS/Audio/Kill Audio Config")]
    public sealed class KillAudioConfig : ScriptableObject
    {
        [Header("普通击杀音效")]
        [SerializeField, InspectorName("Kill Audio Clips")]
        private List<AudioClip> killClips = new List<AudioClip>(8);

        [Header("爆头音效")]
        [SerializeField, InspectorName("HeadShot Audio")]
        private AudioClip headShotClip;

        public IReadOnlyList<AudioClip> KillClips => killClips;
        public AudioClip HeadShotClip => headShotClip;

        public AudioClip GetClip(int killCount, bool isHeadShot)
        {
            if (isHeadShot)
            {
                return headShotClip;
            }

            if (killCount <= 0 || killClips == null || killClips.Count == 0)
            {
                return null;
            }

            int index = Mathf.Clamp(killCount - 1, 0, killClips.Count - 1);
            return killClips[index];
        }
    }
}

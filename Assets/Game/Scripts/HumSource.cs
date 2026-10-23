// @role The girl who hums: 3D off-key anthem you find by listening (fades in only when close)
// @calls AudioSynth
// @tune volume:float:0-1:humming loudness
// @tune maxDistance:float:2-15:from how far you can start to hear her (m)
using UnityEngine;

namespace PhoneGame
{
    /// <summary>A student humming an off-key anthem - you find her by listening.</summary>
    public class HumSource : MonoBehaviour
    {
        public float volume = 0.8f;
        public float maxDistance = 6.5f;
        AudioSource src;

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.clip = AudioSynth.HumMelody();
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 0.8f;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.volume = volume;
            src.playOnAwake = false;
            src.Play();
        }

        public bool Humming { get { return src != null && src.isPlaying; } }

        public void StopHumming()
        {
            if (src != null) src.Stop();
        }
    }
}

// @role 3D footsteps (and the warden's keys) so you can hear where a watcher is
// @calls AudioSynth
// @tune stepLength:float:0.3-1.2:metres per step sound
// @tune volume:float:0-1:footstep loudness
// @tune maxDistance:float:5-30:how far footsteps carry (m)
// @tune keysEverySteps:int:0-10:key jingle every N steps (0 = never)
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Footsteps (3D) for anyone who walks: you can hear where the teacher / monitor / warden is.
    /// Optional key jingle every few steps (the night warden).
    /// </summary>
    public class Footsteps : MonoBehaviour
    {
        public float stepLength = 0.7f;
        public float volume = 0.55f;
        public float pitch = 1f;
        public float minDistance = 1.5f;
        public float maxDistance = 18f;
        [Tooltip("0 = never. Otherwise a key jingle every N steps.")]
        public int keysEverySteps = 0;

        AudioSource src;
        AudioClip[] clips;
        Vector3 last;
        float acc;
        int count;

        void Awake()
        {
            src = gameObject.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.dopplerLevel = 0f;
            src.playOnAwake = false;
            clips = AudioSynth.Steps();
            last = Flat(transform.position);
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void Update()
        {
            Vector3 p = Flat(transform.position);
            float d = Vector3.Distance(p, last);
            last = p;
            if (d > 1f) return;   // teleported
            acc += d;
            if (acc < stepLength) return;
            acc = 0f;
            count++;
            src.pitch = pitch * Random.Range(0.92f, 1.08f);
            src.PlayOneShot(clips[Random.Range(0, clips.Length)], volume);
            if (keysEverySteps > 0 && count % keysEverySteps == 0) src.PlayOneShot(AudioSynth.Keys(), volume * 0.6f);
        }

        /// <summary>Play a one-shot from this person's position (door creak, keys...).</summary>
        public void PlayAt(AudioClip clip, float vol)
        {
            if (src != null && clip != null) src.PlayOneShot(clip, vol);
        }
    }
}

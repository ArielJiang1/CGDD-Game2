// @role Background sound: drone, ventilation, the danger layer that rises when you are seen, sudden silence on a warning
// @calls AudioSynth
// @tune droneVolume:float:0-0.5:background drone level
// @tune ventVolume:float:0-0.5:ventilation hum level
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// The level's background sound: a slow drone + ventilation hum, a dissonant layer that rises with danger,
    /// and "sudden silence" (Duck) when a rule is broken. Also plays 2D one-shots (chimes, stingers, ticks).
    /// Created at runtime by the level director.
    /// </summary>
    public class AmbientAudio : MonoBehaviour
    {
        public float droneVolume = 0.16f;
        public float ventVolume = 0.09f;
        public float extraVolume = 0f;
        [Range(0f, 1f)] public float dangerLevel;

        AudioSource drone, vent, danger, extra, sfx, voice;
        float duck = 1f, duckHold;

        public static AmbientAudio Create(Transform parent)
        {
            GameObject g = new GameObject("AmbientAudio");
            g.transform.SetParent(parent, false);
            return g.AddComponent<AmbientAudio>();
        }

        AudioSource Loop(AudioClip clip)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.spatialBlend = 0f;
            s.playOnAwake = false;
            s.volume = 0f;
            if (clip != null) { s.time = Random.Range(0f, clip.length * 0.9f); s.Play(); }
            return s;
        }

        void Awake()
        {
            drone = Loop(AudioSynth.Drone());
            vent = Loop(AudioSynth.Ventilation());
            danger = Loop(AudioSynth.DangerLayer());
            extra = gameObject.AddComponent<AudioSource>();
            extra.loop = true;
            extra.playOnAwake = false;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            voice = gameObject.AddComponent<AudioSource>();
            voice.playOnAwake = false;
        }

        /// <summary>An extra 2D loop for the level (e.g. sleeping students breathing).</summary>
        public void SetExtraLoop(AudioClip clip, float volume)
        {
            extra.clip = clip;
            extraVolume = volume;
            if (clip != null) extra.Play(); else extra.Stop();
        }

        /// <summary>Everything goes silent for a moment.</summary>
        public void Duck(float seconds) { duckHold = Mathf.Max(duckHold, seconds); }

        public void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;
            if (Mathf.Abs(pitch - 1f) < 0.001f) { sfx.PlayOneShot(clip, volume); return; }
            voice.pitch = pitch;
            voice.PlayOneShot(clip, volume);
        }

        void Update()
        {
            if (duckHold > 0f) duckHold -= Time.unscaledDeltaTime;
            duck = Mathf.MoveTowards(duck, duckHold > 0f ? 0f : 1f, Time.unscaledDeltaTime * (duckHold > 0f ? 6f : 0.6f));
            drone.volume = droneVolume * duck;
            vent.volume = ventVolume * duck;
            danger.volume = Mathf.Clamp01(dangerLevel) * 0.32f * duck;
            extra.volume = extraVolume * duck;
            AudioListener.volume = Mathf.Lerp(0.35f, 1f, duck);   // the whole room goes quiet, not only the music
        }

        void OnDestroy() { AudioListener.volume = 1f; }
    }
}

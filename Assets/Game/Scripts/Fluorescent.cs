// @role Ceiling fluorescent light with buzz; neglected ones flicker
// @calls AudioSynth
// @tune buzzVolume:float:0-0.3:fluorescent buzz loudness
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// A ceiling fluorescent fixture: light + glowing tube + 3D buzz.
    /// "Flicker" fixtures (neglected areas) drop out at random, buzz louder and crackle.
    /// </summary>
    public class Fluorescent : MonoBehaviour
    {
        public Light lightSource;
        public Renderer tube;
        public bool flicker;
        public Color emission = new Color(1.6f, 1.7f, 1.75f);
        public float buzzVolume = 0.07f;
        [Tooltip("0 = off. Lets a level dim or switch off the lights.")]
        [Range(0f, 1f)] public float master = 1f;

        float baseIntensity, level = 1f, offUntil, nextEvent;
        MaterialPropertyBlock mpb;
        AudioSource buzz;

        void Awake()
        {
            if (lightSource != null) baseIntensity = lightSource.intensity;
            mpb = new MaterialPropertyBlock();
            buzz = gameObject.AddComponent<AudioSource>();
            buzz.clip = AudioSynth.FluorescentHum();
            buzz.loop = true;
            buzz.spatialBlend = 1f;
            buzz.rolloffMode = AudioRolloffMode.Linear;
            buzz.minDistance = 0.6f;
            buzz.maxDistance = 7f;
            buzz.dopplerLevel = 0f;
            buzz.volume = buzzVolume;
            buzz.pitch = Random.Range(0.97f, 1.03f);
            buzz.playOnAwake = false;
            buzz.time = Random.Range(0f, 1.9f);
            buzz.Play();
            nextEvent = Time.time + Random.Range(0.5f, 4f);
        }

        void Update()
        {
            float target = 1f;
            if (flicker)
            {
                if (Time.time > nextEvent)
                {
                    // a burst of 1-4 quick drop-outs, then a pause
                    offUntil = Time.time + Random.Range(0.04f, 0.18f);
                    nextEvent = Time.time + (Random.value < 0.6f ? Random.Range(0.08f, 0.35f) : Random.Range(1.5f, 6f));
                }
                if (Time.time < offUntil) target = Random.value < 0.3f ? 0.25f : 0.02f;
            }
            level = target;
            float k = level * master;
            if (lightSource != null) lightSource.intensity = baseIntensity * k;
            if (tube != null)
            {
                tube.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", emission * k);
                tube.SetPropertyBlock(mpb);
            }
            buzz.volume = buzzVolume * master * (flicker && level < 0.5f ? 2.2f : 1f);
        }
    }
}

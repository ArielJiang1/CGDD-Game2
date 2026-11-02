// @role A screen showing nothing but snow ("LIVE" with nobody on it) - flickering grey emission
using UnityEngine;

namespace PhoneGame
{
    /// <summary>An empty "live" broadcast: the screen flickers between greys, now and then a roll bar.</summary>
    public class TVStatic : MonoBehaviour
    {
        public Renderer screen;
        public Light glow;
        public float strength = 1.6f;
        MaterialPropertyBlock mpb;
        float next;

        void Awake()
        {
            if (screen == null) screen = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (screen == null || Time.time < next) return;
            next = Time.time + Random.Range(0.03f, 0.07f);
            float v = Random.Range(0.35f, 1f) * strength;
            if (Random.value < 0.04f) v *= 0.3f;   // the picture rolls
            Color c = new Color(v * 0.9f, v * 0.95f, v);
            screen.GetPropertyBlock(mpb);
            mpb.SetColor("_EmissionColor", c);
            mpb.SetColor("_BaseColor", new Color(0.5f, 0.5f, 0.52f));
            screen.SetPropertyBlock(mpb);
            if (glow != null) glow.intensity = 0.4f + v * 0.35f;
        }
    }
}

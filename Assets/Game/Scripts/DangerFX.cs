// @role Red vignette and colour fringing that grow while you are being seen
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PhoneGame
{
    /// <summary>
    /// Screen-side danger feedback (URP post-processing, built at runtime):
    /// a deep-red vignette + chromatic aberration that grow while you are being seen,
    /// and a hard red flash when a warning is given. Also darkens the screen while lying down.
    /// </summary>
    public class DangerFX : MonoBehaviour
    {
        [Range(0f, 1f)] public float level;
        Volume vol;
        Vignette vig;
        ChromaticAberration ca;
        float flash;

        public static DangerFX Create(Transform parent)
        {
            GameObject g = new GameObject("DangerFX");
            g.transform.SetParent(parent, false);
            return g.AddComponent<DangerFX>();
        }

        void Awake()
        {
            vol = gameObject.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100f;
            VolumeProfile p = ScriptableObject.CreateInstance<VolumeProfile>();
            vig = p.Add<Vignette>(false);
            vig.color.Override(new Color(0.55f, 0f, 0.02f));
            vig.intensity.Override(0.52f);
            vig.smoothness.Override(0.8f);
            ca = p.Add<ChromaticAberration>(false);
            ca.intensity.Override(0.75f);
            vol.sharedProfile = p;
            vol.weight = 0f;
        }

        public void Flash() { flash = 1f; }

        void Update()
        {
            if (flash > 0f) flash -= Time.unscaledDeltaTime * 1.1f;
            vol.weight = Mathf.Clamp01(level * level * 0.9f + Mathf.Max(0f, flash));
        }
    }
}

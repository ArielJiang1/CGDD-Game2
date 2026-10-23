using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Photos-app code puzzle. A phone-shaped outline (with a notch at one end) is scratched into the desk top.
    /// Lay the phone on the desk and rotate it (A / D or mouse). Only when it sits exactly in the outline,
    /// notch to notch, does the screen's glow catch the faint scratched digits next to it: 4 3 1 2.
    /// While the phone lies on the desk the teacher can see it.
    /// </summary>
    public class DeskPuzzle : MonoBehaviour
    {
        public Transform phoneModel;
        [Tooltip("Renderers of the scratched digits that light up when aligned.")]
        public Renderer[] digitRenderers;
        public Color hiddenColor = new Color(0.62f, 0.52f, 0.38f);
        public Color litColor = new Color(1f, 0.9f, 0.65f);
        [Tooltip("Degrees from perfect that still counts as aligned.")]
        public float tolerance = 4f;
        public float keyRotateSpeed = 60f;
        public float mouseRotate = 3f;

        public bool Active { get; private set; }
        public bool Aligned { get; private set; }

        float yaw;
        Material digitMat;

        void Awake()
        {
            if (phoneModel != null) phoneModel.gameObject.SetActive(false);
            if (digitRenderers != null && digitRenderers.Length > 0 && digitRenderers[0] != null)
            {
                digitMat = new Material(digitRenderers[0].sharedMaterial);   // runtime copy, the asset is never changed
                foreach (Renderer r in digitRenderers) if (r != null) r.sharedMaterial = digitMat;
            }
            SetGlow(0f);
        }

        public void Begin()
        {
            Active = true;
            yaw = Random.Range(60f, 150f) * (Random.value < 0.5f ? -1f : 1f);
            if (phoneModel != null) phoneModel.gameObject.SetActive(true);
            Apply();
        }

        public void End()
        {
            Active = false;
            Aligned = false;
            if (phoneModel != null) phoneModel.gameObject.SetActive(false);
            SetGlow(0f);
        }

        void Update()
        {
            if (!Active) return;
            float d = 0f;
            if (GameInput.LeftKeyHeld()) d -= keyRotateSpeed * Time.deltaTime;
            if (GameInput.RightKeyHeld()) d += keyRotateSpeed * Time.deltaTime;
            d += GameInput.MouseDelta().x * mouseRotate;
            yaw = Mathf.Repeat(yaw + d + 180f, 360f) - 180f;
            Apply();
        }

        void Apply()
        {
            if (phoneModel != null) phoneModel.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float err = Mathf.Abs(Mathf.DeltaAngle(yaw, 0f));
            float glow = Mathf.Clamp01(1f - (err - 1f) / 8f);
            SetGlow(glow * glow);
            Aligned = err < tolerance;
        }

        void SetGlow(float t)
        {
            if (digitMat == null) return;
            Color c = Color.Lerp(hiddenColor, litColor, t);
            if (digitMat.HasProperty("_BaseColor")) digitMat.SetColor("_BaseColor", c);
            if (digitMat.HasProperty("_Color")) digitMat.SetColor("_Color", c);
            if (digitMat.HasProperty("_EmissionColor"))
            {
                digitMat.EnableKeyword("_EMISSION");
                digitMat.SetColor("_EmissionColor", litColor * (t * 1.5f));
            }
        }
    }
}

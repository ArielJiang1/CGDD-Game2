using UnityEngine;

namespace Blockout
{
    /// <summary>
    /// Swings an object left/right around its starting Y rotation.
    /// Used by the cafeteria security camera (Challenge 4: "the environment itself can watch").
    /// </summary>
    public class SweepRotate : MonoBehaviour
    {
        [Tooltip("Degrees to each side of the starting direction.")]
        public float amplitude = 35f;

        [Tooltip("Seconds for one full left-right-left sweep.")]
        public float period = 10f;

        float baseYaw;

        void Start()
        {
            baseYaw = transform.localEulerAngles.y;
        }

        void Update()
        {
            if (period <= 0.01f) return;
            float t = Time.time * 2f * Mathf.PI / period;
            Vector3 e = transform.localEulerAngles;
            e.y = baseYaw + Mathf.Sin(t) * amplitude;
            transform.localEulerAngles = e;
        }
    }
}

using UnityEngine;

namespace PhoneGame
{
    /// <summary>Recording LED on a camera / speaker: blinks on and off.</summary>
    public class BlinkingLight : MonoBehaviour
    {
        public Renderer target;
        public float period = 1.2f;
        [Range(0f, 1f)] public float onFraction = 0.5f;
        float offset;

        void Awake()
        {
            if (target == null) target = GetComponent<Renderer>();
            offset = Random.Range(0f, period);
        }

        void Update()
        {
            if (target == null) return;
            target.enabled = Mathf.Repeat(Time.time + offset, period) < period * onFraction;
        }
    }
}

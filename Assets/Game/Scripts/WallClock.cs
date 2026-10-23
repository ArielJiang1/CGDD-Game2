using UnityEngine;

namespace PhoneGame
{
    /// <summary>Institutional wall clock. Hands turn around the clock's local Z axis.</summary>
    public class WallClock : MonoBehaviour
    {
        public Transform hourHand;
        public Transform minuteHand;
        [Tooltip("Time shown when the level starts, in hours (8.9167 = 8:55).")]
        public float startHour = 8.9167f;
        [Tooltip("Game minutes per real second. 1/60 = real time.")]
        public float minutesPerSecond = 1f / 60f;

        float t0;

        void Start() { t0 = Time.time; }

        void Update()
        {
            float hours = startHour + (Time.time - t0) * minutesPerSecond / 60f;
            float minutes = (hours - Mathf.Floor(hours)) * 60f;
            if (hourHand != null) hourHand.localRotation = Quaternion.Euler(0f, 0f, -(hours % 12f) * 30f);
            if (minuteHand != null) minuteHand.localRotation = Quaternion.Euler(0f, 0f, -minutes * 6f);
        }
    }
}

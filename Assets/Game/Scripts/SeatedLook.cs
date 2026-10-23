using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// The player is seated: the body never moves, the head turns (Watch).
    /// Turning is limited like a real seated person - you cannot look straight behind you.
    /// </summary>
    public class SeatedLook : MonoBehaviour
    {
        public Transform eye;
        public float sensitivity = 5f;
        public float maxYaw = 100f;
        public float minPitch = -55f;
        public float maxPitch = 60f;
        public bool canLook = true;

        float yaw, pitch;

        void Update()
        {
            if (!canLook || eye == null) return;
            Vector2 d = GameInput.MouseDelta() * sensitivity;
            yaw = Mathf.Clamp(yaw + d.x, -maxYaw, maxYaw);
            pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
            eye.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}

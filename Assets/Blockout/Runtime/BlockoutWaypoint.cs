using UnityEngine;

namespace Blockout
{
    /// <summary>
    /// One stop on a watcher's patrol route.
    /// Rotate the waypoint to set where the watcher LOOKS while waiting here.
    /// A long wait facing away from the class = the player's "safe window" (Watch -> Peek -> Act -> Hide).
    /// </summary>
    public class BlockoutWaypoint : MonoBehaviour
    {
        [Tooltip("Seconds the watcher stands here, facing this waypoint's forward (blue Z arrow).")]
        public float waitSeconds = 0f;

        [TextArea] public string note = "";

        void OnDrawGizmos()
        {
            Vector3 p = transform.position + Vector3.up * 0.1f;
            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            Gizmos.DrawWireSphere(p, 0.3f);
            if (waitSeconds > 0f)
            {
                Gizmos.color = new Color(1f, 0.25f, 0.2f);
                Gizmos.DrawLine(p, p + transform.forward * 1.2f);
            }
        }
    }
}

// @role Can a watcher see a point: range + view cone + line of sight (classmates, pillars and desks are cover)
// @tune viewAngle:float:30-180:width of the view cone (degrees)
// @tune viewRange:float:3-20:how far he can see (m)
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Can this watcher (teacher, monitor, warden, security camera) see a point? Three checks:
    ///   1. within range   2. inside the view cone (in front of him)   3. nothing solid in between
    /// (classmates, book stacks, desks, pillars all block - that is what "cover" means mechanically).
    /// 'ignoreRoot' = the player: their own body never blocks the view of their own phone.
    /// </summary>
    public class TeacherVision : MonoBehaviour
    {
        [Tooltip("Full width of the view cone in degrees.")]
        public float viewAngle = 90f;
        public float viewRange = 12f;
        [Tooltip("Height of his eyes above his feet (0 for a camera head: the eye is the object itself).")]
        public float eyeHeight = 1.6f;
        [Tooltip("Off = this watcher sees nothing (switched off).")]
        public bool active = true;

        public Vector3 EyePosition { get { return transform.position + Vector3.up * eyeHeight; } }

        public bool CanSee(Vector3 point) { return CanSee(point, null, -1f); }

        public bool CanSee(Vector3 point, Transform ignoreRoot, float rangeOverride = -1f)
        {
            if (!active || !isActiveAndEnabled) return false;
            Vector3 eye = EyePosition;
            Vector3 to = point - eye;
            float dist = to.magnitude;
            float range = rangeOverride > 0f ? rangeOverride : viewRange;
            if (dist > range || dist < 0.001f) return false;

            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 fwd = transform.forward; fwd.y = 0f;
            if (flat.sqrMagnitude > 0.0001f && fwd.sqrMagnitude > 0.0001f && Vector3.Angle(fwd, flat) > viewAngle * 0.5f) return false;

            RaycastHit[] hits = Physics.RaycastAll(eye, to / dist, dist - 0.02f, ~0, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit h in hits)
            {
                if (h.transform.IsChildOf(transform)) continue;
                if (ignoreRoot != null && h.transform.IsChildOf(ignoreRoot)) continue;
                return false;   // something is in the way
            }
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            Vector3 e = EyePosition;
            Quaternion l = Quaternion.Euler(0f, -viewAngle * 0.5f, 0f), r = Quaternion.Euler(0f, viewAngle * 0.5f, 0f);
            Gizmos.DrawLine(e, e + l * transform.forward * viewRange);
            Gizmos.DrawLine(e, e + r * transform.forward * viewRange);
        }
    }
}

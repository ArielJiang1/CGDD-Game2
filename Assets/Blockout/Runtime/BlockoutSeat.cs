using UnityEngine;

namespace Blockout
{
    /// <summary>
    /// Makes a chair sittable by the BlockoutWalker. Each chair has exactly ONE seat point.
    ///
    /// Occupancy is detected at runtime with a small physics probe where a sitter's torso would be,
    /// so if you move classmates around by hand, the chairs update automatically:
    ///   - a classmate (or anything else solid) in the seat  -> "Seat taken"
    ///   - the yellow Fig_Seated_Player placeholder          -> sittable; the placeholder hides while you sit
    /// </summary>
    public class BlockoutSeat : MonoBehaviour
    {
        [Tooltip("Where the sitter's feet/pivot go, in the chair's local space (floor level, centre of the seat).")]
        public Vector3 localSeatPoint = Vector3.zero;

        [Tooltip("Eye height above the floor while seated. 1.25 m = the eye strip on every seated NPC figure.")]
        public float eyeHeight = 1.25f;

        [Tooltip("How far (degrees) the head can turn left/right from the chair's facing while seated.")]
        public float maxHeadTurn = 110f;

        const string PlaceholderPrefix = "Fig_Seated_Player";

        public Vector3 SeatPosition { get { return transform.TransformPoint(localSeatPoint); } }
        public float SeatYaw { get { return transform.eulerAngles.y; } }

        /// <summary>True if someone else is sitting here. Returns the player placeholder (if any) so it can be hidden.</summary>
        public bool IsTaken(Transform ignore, out GameObject placeholder)
        {
            placeholder = null;
            Vector3 probe = SeatPosition + Vector3.up * 0.8f;   // torso height of a seated figure
            foreach (Collider c in Physics.OverlapSphere(probe, 0.1f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (c.transform.IsChildOf(transform)) continue;
                if (ignore != null && c.transform.IsChildOf(ignore)) continue;
                Transform ph = FindParentNamed(c.transform, PlaceholderPrefix);
                if (ph != null) { placeholder = ph.gameObject; continue; }
                return true;
            }
            return false;
        }

        static Transform FindParentNamed(Transform t, string prefix)
        {
            while (t != null)
            {
                if (t.name.StartsWith(prefix)) return t;
                t = t.parent;
            }
            return null;
        }

        void OnDrawGizmos()
        {
            Vector3 p = SeatPosition;
            Gizmos.color = new Color(1f, 0.8f, 0.1f);
            Gizmos.DrawWireSphere(p + Vector3.up * 0.45f, 0.08f);
            Gizmos.DrawLine(p + Vector3.up * 0.45f, p + Vector3.up * eyeHeight);
            Gizmos.DrawLine(p + Vector3.up * eyeHeight, p + Vector3.up * eyeHeight + transform.forward * 0.4f);
        }
    }
}

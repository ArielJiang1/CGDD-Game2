using UnityEngine;

namespace Blockout
{
    /// <summary>
    /// Blockout-only patrol: walks the watcher between waypoints, pauses, and faces the
    /// waypoint's forward direction while paused. No AI, no detection - it only exists so the
    /// patrol RHYTHM (Challenge 1: "a patrolling teacher with a learnable rhythm") can be
    /// seen and timed in the fly-through, and the route can be changed cheaply.
    /// </summary>
    public class TeacherPatrol : MonoBehaviour
    {
        public BlockoutWaypoint[] waypoints = new BlockoutWaypoint[0];

        [Tooltip("Metres per second. ~1.2 is a slow classroom stroll.")]
        public float walkSpeed = 1.2f;

        [Tooltip("Degrees per second.")]
        public float turnSpeed = 240f;

        [Tooltip("Waypoint the patrol starts from.")]
        public int startIndex = 0;

        [Tooltip("On: snap to the start waypoint and wait there. Off: walk to it from where he stands now.")]
        public bool teleportToStart = true;

        [Header("Read-only (for playtest notes)")]
        public string currentNote = "";

        int index;
        bool waiting;
        float waitTimer;

        void Start()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            index = Mathf.Clamp(startIndex, 0, waypoints.Length - 1);
            if (teleportToStart)
            {
                transform.position = waypoints[index].transform.position;
                BeginWait(waypoints[index]);
            }
            else
            {
                waiting = false;
                currentNote = "walking";
            }
        }

        void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            BlockoutWaypoint wp = waypoints[index];
            if (wp == null) { Advance(); return; }

            if (waiting)
            {
                Vector3 look = Flat(wp.transform.forward);
                if (look.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(look);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
                }
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f) Advance();
                return;
            }

            Vector3 goal = wp.transform.position;
            Vector3 dir = Flat(goal - transform.position);
            if (dir.sqrMagnitude > 0.0001f)
            {
                Quaternion target = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
            transform.position = Vector3.MoveTowards(transform.position, goal, walkSpeed * Time.deltaTime);
            if ((transform.position - goal).sqrMagnitude < 0.0004f) BeginWait(wp);
        }

        void BeginWait(BlockoutWaypoint wp)
        {
            waiting = true;
            waitTimer = wp.waitSeconds;
            currentNote = wp.note;
        }

        void Advance()
        {
            waiting = false;
            index = (index + 1) % waypoints.Length;
            currentNote = "walking";
        }

        static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

        void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;
            Gizmos.color = new Color(1f, 0.55f, 0.1f);
            for (int i = 0; i < waypoints.Length; i++)
            {
                BlockoutWaypoint a = waypoints[i];
                BlockoutWaypoint b = waypoints[(i + 1) % waypoints.Length];
                if (a == null || b == null) continue;
                Gizmos.DrawLine(a.transform.position + Vector3.up * 0.1f, b.transform.position + Vector3.up * 0.1f);
            }
        }
    }
}

// @role The teacher on a NavMesh: lectures, writes on the board (back turned = safe), wanders, calls on you, comes to take the phone
// @calls TeacherVision
// @tune lectureWeight:float:0-1:chance he lectures facing the class
// @tune boardWeight:float:0-1:chance he turns to the board (safe window)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PhoneGame
{
    /// <summary>
    /// The teacher. Walks on a NavMesh (so he goes AROUND desks and students, never through them) and
    /// picks his next activity at random:
    ///   LECTURE    - stands at the front facing the class (dangerous)
    ///   BOARD      - writes on the blackboard with his back to the class (safe window)
    ///   WANDER     - walks to a random spot in the room, then looks around
    /// The level director can interrupt him to CALL ON the player, or to come and TAKE the phone.
    /// What he SAYS is in LessonScript (lines + questions) and is shown as subtitles by the director.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class TeacherAI : MonoBehaviour
    {
        public enum Activity { Idle, Lecture, Board, Wander, LookAround, Calling, Approach }

        [Header("Spots (world positions on the classroom floor)")]
        public Vector3 lectureSpot = new Vector3(3.2f, 0.15f, 10.0f);
        public float lectureFacingYaw = 180f;
        public Vector3 boardSpot = new Vector3(3.6f, 0.15f, 11.3f);
        public float boardFacingYaw = 0f;
        [Tooltip("Random wander points are picked inside this rectangle (x min, z min, x max, z max).")]
        public Vector4 roomRect = new Vector4(0.4f, 0.5f, 9.6f, 11.4f);

        [Header("Timing (seconds)")]
        public Vector2 lectureTime = new Vector2(7f, 11f);
        public Vector2 boardTime = new Vector2(5f, 8f);
        public Vector2 lookAroundTime = new Vector2(2f, 4f);
        [Range(0f, 1f)] public float lectureWeight = 0.35f;
        [Range(0f, 1f)] public float boardWeight = 0.3f;

        [Header("Speech")]
        public List<LessonLine> lines = LessonDefaults.Lines();
        public List<LessonQuestion> questions = LessonDefaults.Questions();

        public Activity Current { get; private set; }
        public bool Running { get; private set; }
        /// <summary>True when he is facing the board (his back to the class).</summary>
        public bool IsWritingOnBoard { get { return Current == Activity.Board && arrived; } }

        NavMeshAgent agent;
        float timer;
        bool arrived;
        float targetYaw;
        bool hasTargetYaw;
        Transform faceTarget;
        float sinceGo;
        Transform visual;   // all his visible parts; kept on the real floor (the NavMesh floats a little above it)

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = false;   // enabled after the NavMesh is built

            visual = new GameObject("Visual").transform;
            List<Transform> kids = new List<Transform>();
            foreach (Transform c in transform) kids.Add(c);
            visual.SetParent(transform, false);
            foreach (Transform c in kids) c.SetParent(visual, false);
        }

        /// <summary>Call after the NavMesh exists.</summary>
        public void Begin()
        {
            agent.enabled = true;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas)) agent.Warp(hit.position);
            Running = true;
            StartActivity(Activity.Board);
        }

        public void CallOn(Transform player)
        {
            Current = Activity.Calling;
            faceTarget = player;
            hasTargetYaw = false;
            Stop();
        }

        public void EndCall()
        {
            faceTarget = null;
            PickNext();
        }

        /// <summary>Walk over to the player (used when taking the phone away).</summary>
        public void Approach(Vector3 standPoint, Transform lookAt)
        {
            Current = Activity.Approach;
            faceTarget = lookAt;
            Go(standPoint);
        }

        public bool HasArrived { get { return arrived; } }

        /// <summary>Go back to his normal random lesson routine.</summary>
        public void Resume() { EndCall(); }

        /// <summary>Stop where he is and face a direction (e.g. the board) until Resume().</summary>
        public void HoldFacing(float yaw)
        {
            Current = Activity.Calling;
            faceTarget = null;
            hasTargetYaw = true;
            targetYaw = yaw;
            Stop();
        }

        /// <summary>Walk to a point, then face a direction and stay there until Resume().</summary>
        public void WalkToAndFace(Vector3 point, float yaw)
        {
            Current = Activity.Approach;
            faceTarget = null;
            hasTargetYaw = true;
            targetYaw = yaw;
            Go(point);
        }

        void Update()
        {
            if (!Running || !agent.enabled) return;

            sinceGo += Time.deltaTime;
            if (!arrived && sinceGo > 0.3f && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                arrived = true;
                agent.isStopped = true;
                timer = DurationFor(Current);
            }

            if (arrived || Current == Activity.Calling)
            {
                Vector3 dir = Vector3.zero;
                if (faceTarget != null) { dir = faceTarget.position - transform.position; dir.y = 0f; }
                else if (hasTargetYaw) dir = Quaternion.Euler(0f, targetYaw, 0f) * Vector3.forward;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 220f * Time.deltaTime);
            }

            if (Current == Activity.Calling || Current == Activity.Approach) return;

            if (arrived)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    if (Current == Activity.Wander) StartActivity(Activity.LookAround);
                    else PickNext();
                }
            }
        }

        void LateUpdate()
        {
            if (visual == null) return;
            RaycastHit hit;
            Vector3 from = transform.position + Vector3.up * 0.8f;
            if (Physics.Raycast(from, Vector3.down, out hit, 2f, ~(1 << gameObject.layer), QueryTriggerInteraction.Ignore))
            {
                // only small corrections: never climb onto a chair, bench or desk the ray happens to hit
                float want = Mathf.Clamp(hit.point.y - transform.position.y, -0.12f, 0.17f);
                Vector3 lp = visual.localPosition;
                lp.y = Mathf.MoveTowards(lp.y, want, 1f * Time.deltaTime);
                visual.localPosition = lp;
            }
        }

        void PickNext()
        {
            float r = Random.value;
            if (r < lectureWeight) StartActivity(Activity.Lecture);
            else if (r < lectureWeight + boardWeight) StartActivity(Activity.Board);
            else StartActivity(Activity.Wander);
        }

        void StartActivity(Activity a)
        {
            Current = a;
            faceTarget = null;
            switch (a)
            {
                case Activity.Lecture:
                    hasTargetYaw = true; targetYaw = lectureFacingYaw;
                    Go(lectureSpot);
                    break;
                case Activity.Board:
                    hasTargetYaw = true; targetYaw = boardFacingYaw;
                    Go(boardSpot);
                    break;
                case Activity.Wander:
                    hasTargetYaw = false;
                    Go(RandomPoint());
                    break;
                case Activity.LookAround:
                    hasTargetYaw = true; targetYaw = Random.Range(0f, 360f);
                    arrived = true;
                    timer = DurationFor(a);
                    break;
            }
        }

        void Go(Vector3 p)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(p, out hit, 1.0f, NavMesh.AllAreas)) p = hit.position;
            agent.isStopped = false;
            agent.SetDestination(p);
            arrived = false;
            sinceGo = 0f;
        }

        void Stop()
        {
            if (agent.enabled) { agent.isStopped = true; agent.ResetPath(); }
            arrived = true;
        }

        Vector3 RandomPoint()
        {
            for (int i = 0; i < 20; i++)
            {
                Vector3 p = new Vector3(Random.Range(roomRect.x, roomRect.z), transform.position.y, Random.Range(roomRect.y, roomRect.w));
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(p, out hit, 0.6f, NavMesh.AllAreas)) continue;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
            }
            return lectureSpot;
        }

        float DurationFor(Activity a)
        {
            switch (a)
            {
                case Activity.Lecture: return Random.Range(lectureTime.x, lectureTime.y);
                case Activity.Board: return Random.Range(boardTime.x, boardTime.y);
                case Activity.LookAround: return Random.Range(lookAroundTime.x, lookAroundTime.y);
                case Activity.Wander: return 0.2f;
                default: return 1f;
            }
        }
    }
}

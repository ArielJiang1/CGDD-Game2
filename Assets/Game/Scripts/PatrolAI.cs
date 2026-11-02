// @role A walking watcher on a NavMesh (Lunch Monitor, night warden): roams, follows scripted routes, scans while waiting
// @calls TeacherVision
// @tune scanAngle:float:0-90:how far he looks left/right while standing
// @tune scanSpeed:float:0.1-2:how fast he looks around
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace PhoneGame
{
    /// <summary>
    /// A walking watcher on a NavMesh (lunch monitor, night warden): goes around furniture, never through it.
    ///   ROAM  - picks random points inside roamRect, waits there and looks around.
    ///   ROUTE - follows a list of points given by the level (waits + facing per point), then reports back.
    ///   APPROACH / HOLD - the level takes over (coming to take the phone, staring at you...).
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class PatrolAI : MonoBehaviour
    {
        [Tooltip("Random points are picked inside this rectangle (x min, z min, x max, z max).")]
        public Vector4 roamRect = new Vector4(15f, 0.8f, 29f, 9.5f);
        public Vector2 waitTime = new Vector2(2f, 4.5f);
        [Tooltip("While waiting, the head sweeps left/right by this many degrees.")]
        public float scanAngle = 55f;
        public float scanSpeed = 0.6f;
        public bool roamOnBegin = true;

        public bool Running { get; private set; }
        public bool HasArrived { get { return arrived; } }
        public bool RouteDone { get { return route == null; } }

        NavMeshAgent agent;
        Transform visual;
        bool arrived, holding, waiting;
        float waitLeft, baseYaw, scanT, sinceGo;
        Transform lookAt;
        Vector3[] route;
        float[] routeWaits, routeYaws;
        int routeIndex;
        System.Action onRouteDone;

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

        public void Begin()
        {
            agent.enabled = true;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas)) agent.Warp(hit.position);
            Running = true;
            if (roamOnBegin) NextRoam();
            else { arrived = true; waiting = true; waitLeft = 9999f; baseYaw = transform.eulerAngles.y; }
        }

        /// <summary>Walk a list of points. waits[i] = seconds at point i; yaws[i] = facing there (NaN = keep).</summary>
        [Tooltip("Speeds up the pauses at each point (story speed 2x sets this to 2).")]
        public float timeScale = 1f;

        public void RunRoute(Vector3[] points, float[] waits, float[] yaws, System.Action done)
        {
            holding = false;
            lookAt = null;
            route = points;
            routeWaits = waits;
            routeYaws = yaws;
            routeIndex = 0;
            onRouteDone = done;
            Go(route[0]);
        }

        public void Approach(Vector3 point, Transform look)
        {
            route = null;
            holding = true;
            lookAt = look;
            Go(point);
        }

        public void Hold(Transform look)
        {
            route = null;
            holding = true;
            lookAt = look;
            Stop();
        }

        public void ResumeRoam()
        {
            holding = false;
            lookAt = null;
            route = null;
            NextRoam();
        }

        void Update()
        {
            if (!Running || !agent.enabled) return;
            sinceGo += Time.deltaTime;

            if (!arrived && sinceGo > 0.3f && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
            {
                arrived = true;
                agent.isStopped = true;
                OnArrive();
            }

            if (holding)
            {
                if (arrived && lookAt != null) Face(lookAt.position - transform.position, 220f);
                return;
            }

            if (arrived && waiting)
            {
                scanT += Time.deltaTime * scanSpeed;
                float yawNow = baseYaw + Mathf.Sin(scanT * Mathf.PI) * scanAngle;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, yawNow, 0f), 140f * Time.deltaTime);
                waitLeft -= Time.deltaTime * timeScale;
                if (waitLeft <= 0f) Next();
            }
        }

        void OnArrive()
        {
            if (holding) return;
            waiting = true;
            scanT = 0f;
            baseYaw = transform.eulerAngles.y;
            if (route != null)
            {
                float y = (routeYaws != null && routeIndex < routeYaws.Length) ? routeYaws[routeIndex] : float.NaN;
                if (!float.IsNaN(y)) baseYaw = y;
                waitLeft = (routeWaits != null && routeIndex < routeWaits.Length) ? routeWaits[routeIndex] : 0f;
            }
            else waitLeft = Random.Range(waitTime.x, waitTime.y);
        }

        void Next()
        {
            waiting = false;
            if (route != null)
            {
                routeIndex++;
                if (routeIndex < route.Length) { Go(route[routeIndex]); return; }
                route = null;
                System.Action cb = onRouteDone;
                onRouteDone = null;
                arrived = true; waiting = true; waitLeft = 9999f;
                if (cb != null) cb();
                return;
            }
            NextRoam();
        }

        void NextRoam() { Go(RandomPoint()); }

        void Face(Vector3 dir, float speed)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), speed * Time.deltaTime);
        }

        void Go(Vector3 p)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(p, out hit, 1.2f, NavMesh.AllAreas)) p = hit.position;
            agent.isStopped = false;
            agent.SetDestination(p);
            arrived = false;
            waiting = false;
            sinceGo = 0f;
        }

        void Stop()
        {
            if (agent.enabled) { agent.isStopped = true; agent.ResetPath(); }
            arrived = true;
        }

        Vector3 RandomPoint()
        {
            for (int i = 0; i < 25; i++)
            {
                Vector3 p = new Vector3(Random.Range(roamRect.x, roamRect.z), transform.position.y, Random.Range(roamRect.y, roamRect.w));
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(p, out hit, 0.6f, NavMesh.AllAreas)) continue;
                NavMeshPath path = new NavMeshPath();
                if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete) return hit.position;
            }
            return transform.position;
        }

        void LateUpdate()
        {
            // keep the visible body on the real floor (the NavMesh floats a few cm above it)
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
    }
}

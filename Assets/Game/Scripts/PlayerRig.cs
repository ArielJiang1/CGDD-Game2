// @role The player body: walk, pulled to the seat, seated look, inspect the desk cubby, lie down in bed
// @tune walkSpeed:float:1-5:walking speed (m/s)
// @tune sensitivity:float:1-10:mouse look speed
// @tune pullSpeed:float:0.5-3:speed when the class watches you walk to your seat
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// The main character.
    ///   WALK    - free first-person walking (hallway, cafeteria).
    ///   PULLED  - the game takes control and walks you somewhere (to your seat).
    ///   SEATED  - can't get up by yourself. Mouse looks around (head turn is limited).
    ///   INSPECT - leaning down to look inside the desk cubby.
    ///   PLACING - looking down at the desk top.
    ///   LYING   - lying in bed, pretending to sleep (Lights Out).
    ///   LOCKED  - no control (cutscenes, game over).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerRig : MonoBehaviour
    {
        public enum Mode { Walk, Pulled, Seated, Inspect, Placing, Lying, Locked }

        public Transform eye;
        [Header("Walking")]
        public float walkSpeed = 2.8f;
        public float runSpeed = 4.2f;
        public float gravity = -20f;
        public float standingEye = 1.5f;
        public float pullSpeed = 1.3f;
        [Tooltip("Set by the level, e.g. while you are looking at the phone you stand still.")]
        public bool freezeMovement;
        [Header("Look")]
        public float sensitivity = 5f;
        [Header("Seated eye positions (local to the seat)")]
        public Vector3 seatedEye = new Vector3(0f, 1.25f, 0.05f);   // same eye height as the seated NPCs
        public Vector3 inspectEye = new Vector3(0f, 0.64f, 0.12f);
        public float inspectPitch = 8f;
        public Vector3 placingEye = new Vector3(0f, 1.15f, 0.22f);
        public float placingPitch = 55f;
        [Header("Lying in bed")]
        public Vector3 lyingEye = new Vector3(0f, 0.72f, -0.45f);
        public float lyingPitch = -28f;

        public Mode CurrentMode { get; private set; }

        CharacterController cc;
        float yaw, pitch, vy;
        float headYaw, headPitch, basePitch, basePitchTarget;
        float yawLimit = 100f, pitchMin = -55f, pitchMax = 60f;
        Vector3 eyeTarget;
        Vector3[] path;
        int pathIndex;
        Vector3 seatPosition;
        float seatYaw;
        System.Action onSeated;
        float eyeSpeed = 1.5f;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            CurrentMode = Mode.Walk;
            yaw = transform.eulerAngles.y;
            eyeTarget = new Vector3(0f, standingEye, 0f);
            if (eye != null) eye.localPosition = eyeTarget;
        }

        void Update()
        {
            switch (CurrentMode)
            {
                case Mode.Walk: UpdateWalk(); break;
                case Mode.Pulled: UpdatePull(); break;
                case Mode.Seated:
                case Mode.Inspect:
                case Mode.Lying: UpdateHead(); break;
                case Mode.Placing: ApplyHead(); break;
            }
            if (eye != null) eye.localPosition = Vector3.MoveTowards(eye.localPosition, eyeTarget, eyeSpeed * Time.deltaTime);
        }

        // ------------------------------------------------------------------ walking
        void UpdateWalk()
        {
            Vector2 d = GameInput.MouseDelta() * sensitivity;
            yaw += d.x;
            pitch = Mathf.Clamp(pitch - d.y, -80f, 80f);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (eye != null) eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector2 mv = freezeMovement ? Vector2.zero : GameInput.Move();
            Vector3 dir = transform.right * mv.x + transform.forward * mv.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float speed = GameInput.RunHeld() ? runSpeed : walkSpeed;
            if (!cc.enabled) return;
            if (cc.isGrounded) vy = -1f;
            vy += gravity * Time.deltaTime;
            cc.Move((dir * speed + Vector3.up * vy) * Time.deltaTime);
        }

        /// <summary>Take control away and walk the character along 'points' to the seat.</summary>
        public void PullToSeat(Vector3[] points, Vector3 seatPos, float seatFacingYaw, System.Action seated)
        {
            CurrentMode = Mode.Pulled;
            cc.enabled = false;
            pullSpeedNow = 0.4f;
            path = points;
            pathIndex = 0;
            seatPosition = seatPos;
            seatYaw = seatFacingYaw;
            onSeated = seated;
        }

        [Header("Being pulled to the seat")]
        [Tooltip("Direction the eyes drift towards while walking in (0 = the front of the class).")]
        public float pullLookYaw = 0f;
        [Range(0f, 1f)] public float pullLookBias = 0.55f;
        float pullSpeedNow;

        void UpdatePull()
        {
            if (path == null || pathIndex >= path.Length) { SitDown(); return; }
            bool last = pathIndex == path.Length - 1;
            Vector3 target = path[pathIndex];
            target.y = transform.position.y;
            Vector3 to = target - transform.position;
            float dist = to.magnitude;

            // ease in, ease out on the last stretch
            float top = last ? Mathf.Lerp(0.35f, pullSpeed, Mathf.Clamp01(dist / 0.8f)) : pullSpeed;
            pullSpeedNow = Mathf.MoveTowards(pullSpeedNow, top, 1.2f * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, target, pullSpeedNow * Time.deltaTime);

            // look: partly where you walk, partly at the class staring back at you - never straight into a wall
            if (dist > 0.05f)
            {
                float walkYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float want = Mathf.LerpAngle(walkYaw, pullLookYaw, last ? 0.85f : pullLookBias);
                yaw = Mathf.MoveTowardsAngle(yaw, want, 70f * Time.deltaTime);
            }
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            pitch = Mathf.MoveTowards(pitch, 6f, 30f * Time.deltaTime);
            if (eye != null) eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            // cut corners: move on to the next point a little before reaching this one
            if (dist < (last ? 0.02f : 0.45f)) pathIndex++;
        }

        void SitDown()
        {
            transform.position = seatPosition;
            yaw = seatYaw;
            transform.rotation = Quaternion.Euler(0f, seatYaw, 0f);
            headYaw = 0f; headPitch = pitch; basePitch = 0f; basePitchTarget = 0f;
            BackToSeat();
            if (onSeated != null) onSeated();
        }

        // ------------------------------------------------------------------ seated
        /// <summary>Put the character straight into a seat (level start, bench, bed).</summary>
        public void SitAt(Vector3 pos, float facingYaw)
        {
            cc.enabled = false;
            transform.position = pos;
            yaw = facingYaw;
            transform.rotation = Quaternion.Euler(0f, facingYaw, 0f);
            headYaw = 0f; headPitch = 0f; pitch = 0f; basePitch = 0f; basePitchTarget = 0f;
            BackToSeat();
            if (eye != null) { eye.localPosition = seatedEye; eye.localRotation = Quaternion.identity; }
        }

        /// <summary>Stand up and walk again from 'pos'.</summary>
        public void StandUpAt(Vector3 pos)
        {
            transform.position = pos;
            yaw = transform.eulerAngles.y + headYaw;
            pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cc.enabled = true;
            eyeTarget = new Vector3(0f, standingEye, 0f);
            CurrentMode = Mode.Walk;
        }

        /// <summary>Move a walking character somewhere else instantly (e.g. sent back by the PA).</summary>
        public void Teleport(Vector3 pos, float facingYaw)
        {
            bool was = cc.enabled;
            cc.enabled = false;
            transform.position = pos;
            yaw = facingYaw;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            cc.enabled = was;
        }

        public void BackToSeat()
        {
            CurrentMode = Mode.Seated;
            eyeTarget = seatedEye;
            eyeSpeed = 1.5f;
            basePitchTarget = 0f;
            yawLimit = 100f; pitchMin = -60f; pitchMax = 60f;
        }

        public void EnterInspect()
        {
            CurrentMode = Mode.Inspect;
            eyeTarget = inspectEye;
            basePitchTarget = inspectPitch;
            headYaw = 0f; headPitch = 0f;
            yawLimit = 40f; pitchMin = -25f; pitchMax = 30f;
        }

        public void EnterPlacing()
        {
            CurrentMode = Mode.Placing;
            eyeTarget = placingEye;
            basePitchTarget = placingPitch;
            headYaw = 0f; headPitch = 0f;
        }

        /// <summary>Lie down in bed (pretend to sleep). SitUp() = BackToSeat().</summary>
        public void LieDown()
        {
            CurrentMode = Mode.Lying;
            eyeTarget = lyingEye;
            eyeSpeed = 2.4f;
            basePitchTarget = lyingPitch;
            headYaw = Mathf.Clamp(headYaw, -35f, 35f);
            headPitch = 0f;
            yawLimit = 35f; pitchMin = -15f; pitchMax = 15f;
        }

        public void SitUp() { BackToSeat(); eyeSpeed = 2.4f; }

        public void Lock() { CurrentMode = Mode.Locked; }

        /// <summary>Hand walking control (back) to the player - used at the very start, once the teacher has called you in.</summary>
        public void AllowWalk() { CurrentMode = Mode.Walk; }

        public bool IsSeatedMode { get { return CurrentMode == Mode.Seated || CurrentMode == Mode.Inspect || CurrentMode == Mode.Placing; } }

        /// <summary>Where the head is turned relative to the seat (degrees).</summary>
        public float HeadYaw { get { return headYaw; } }

        void UpdateHead()
        {
            Vector2 d = GameInput.MouseDelta() * sensitivity;
            headYaw = Mathf.Clamp(headYaw + d.x, -yawLimit, yawLimit);
            headPitch = Mathf.Clamp(headPitch - d.y, pitchMin, pitchMax);
            ApplyHead();
        }

        void ApplyHead()
        {
            basePitch = Mathf.MoveTowards(basePitch, basePitchTarget, 150f * Time.deltaTime);
            if (eye != null) eye.localRotation = Quaternion.Euler(basePitch + headPitch, headYaw, 0f);
        }
    }
}

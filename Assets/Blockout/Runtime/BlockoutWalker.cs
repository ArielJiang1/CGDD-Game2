using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace Blockout
{
    /// <summary>
    /// Minimal first-person walker for self-playtesting the blockout "with full player gravity,
    /// collision, and speed" (Level Design Book - Blockout, step 3), instead of only flying.
    ///
    /// Collision is 1.0 m wide x 1.8 m tall (book's Unity metrics). Standing eye 1.5 m
    /// (book: 1.5-1.7 m, games usually sit at the low end).
    ///
    /// Look at a chair and press E to SIT: you snap to the chair's single seat point and your eye drops
    /// to the chair's eyeHeight (1.25 m) - the same height as every seated NPC. Press E again to stand up.
    ///
    /// Controls: hold right mouse (or click to lock) = look, Esc = unlock, WASD = move,
    /// Shift = run, Space = jump, E = sit / stand.
    /// Works with the old Input Manager or the new Input System package.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class BlockoutWalker : MonoBehaviour
    {
        public Transform eye;

        [Header("Movement (m/s)")]
        public float walkSpeed = 4f;
        public float runSpeed = 6.5f;
        public float jumpHeight = 1.0f;
        public float gravity = -20f;

        [Header("View")]
        public float eyeStanding = 1.5f;
        public float lookSensitivity = 5f;

        [Header("Chairs")]
        public float interactDistance = 2.5f;

        CharacterController cc;
        float yaw, pitch, vy;

        BlockoutSeat seat;              // chair we are sitting on (null = standing)
        BlockoutSeat lookedAtSeat;      // chair under the crosshair
        bool lookedAtSeatTaken;
        GameObject hiddenPlaceholder;
        Vector3 standPosition;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            yaw = transform.eulerAngles.y;
            if (eye == null)
            {
                Camera c = GetComponentInChildren<Camera>();
                if (c != null) eye = c.transform;
            }
        }

        void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            if (ClickPressed()) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (EscPressed()) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

            // Look while the mouse is locked (click) OR while the right mouse button is held.
            if (Cursor.lockState == CursorLockMode.Locked || RightHeld())
            {
                Vector2 look = LookDelta() * lookSensitivity;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            }

            if (seat != null) UpdateSeated();
            else UpdateStanding();
        }

        // ------------------------------------------------------------------ seated
        void UpdateSeated()
        {
            float rel = Mathf.Clamp(Mathf.DeltaAngle(seat.SeatYaw, yaw), -seat.maxHeadTurn, seat.maxHeadTurn);
            yaw = seat.SeatYaw + rel;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            MoveEye(seat.eyeHeight);
            if (InteractPressed()) StandUp();
        }

        void SitDown(BlockoutSeat target, GameObject placeholder)
        {
            seat = target;
            standPosition = transform.position;
            cc.enabled = false;                                   // no collisions while seated
            transform.position = target.SeatPosition;
            yaw = target.SeatYaw;
            pitch = 0f;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            vy = 0f;
            hiddenPlaceholder = placeholder;
            if (hiddenPlaceholder != null) hiddenPlaceholder.SetActive(false);
        }

        void StandUp()
        {
            transform.position = standPosition;                   // back to where you stood before sitting
            cc.enabled = true;
            if (hiddenPlaceholder != null) hiddenPlaceholder.SetActive(true);
            hiddenPlaceholder = null;
            seat = null;
        }

        // ------------------------------------------------------------------ standing
        void UpdateStanding()
        {
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            MoveEye(eyeStanding);

            FindSeatUnderCrosshair();
            if (InteractPressed() && lookedAtSeat != null && !lookedAtSeatTaken)
            {
                GameObject placeholder;
                lookedAtSeat.IsTaken(transform, out placeholder);
                SitDown(lookedAtSeat, placeholder);
                return;
            }

            Vector2 mv = MoveInput();
            Vector3 dir = transform.right * mv.x + transform.forward * mv.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float speed = RunHeld() ? runSpeed : walkSpeed;

            if (cc.isGrounded)
            {
                vy = -1f;
                if (JumpPressed()) vy = Mathf.Sqrt(2f * jumpHeight * -gravity);
            }
            vy += gravity * Time.deltaTime;
            cc.Move((dir * speed + Vector3.up * vy) * Time.deltaTime);
        }

        void FindSeatUnderCrosshair()
        {
            lookedAtSeat = null;
            lookedAtSeatTaken = false;
            if (eye == null) return;

            RaycastHit hit;
            if (!Physics.Raycast(eye.position, eye.forward, out hit, interactDistance, ~0, QueryTriggerInteraction.Ignore)) return;

            BlockoutSeat s = hit.collider.GetComponentInParent<BlockoutSeat>();
            if (s == null)
            {
                // Looking at the person sitting in the chair (or the desk)? Pick the nearest chair to the hit point.
                float best = 0.8f;
                foreach (Collider c in Physics.OverlapSphere(hit.point, 0.8f, ~0, QueryTriggerInteraction.Ignore))
                {
                    BlockoutSeat cand = c.GetComponentInParent<BlockoutSeat>();
                    if (cand == null) continue;
                    float d = Vector3.Distance(hit.point, cand.SeatPosition + Vector3.up * 0.6f);
                    if (d < best) { best = d; s = cand; }
                }
            }
            if (s == null) return;

            GameObject placeholder;
            lookedAtSeat = s;
            lookedAtSeatTaken = s.IsTaken(transform, out placeholder);
        }

        void MoveEye(float height)
        {
            if (eye == null) return;
            eye.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            Vector3 p = eye.localPosition;
            p.y = Mathf.MoveTowards(p.y, height, 3f * Time.deltaTime);
            eye.localPosition = p;
        }

        // ------------------------------------------------------------------ HUD
        void OnGUI()
        {
            // crosshair
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width * 0.5f - 5f, Screen.height * 0.5f - 11f, 20f, 22f), "+");

            // interaction prompt
            string prompt = null;
            if (seat != null) prompt = "[E] Stand up";
            else if (lookedAtSeat != null) prompt = lookedAtSeatTaken ? "Seat taken" : "[E] Sit";
            if (prompt != null)
                GUI.Box(new Rect(Screen.width * 0.5f - 70f, Screen.height * 0.5f + 20f, 140f, 24f), prompt);

            // status (bottom-left, clear of FlyCamera's hint)
            Camera cam = eye != null ? eye.GetComponent<Camera>() : null;
            bool camOff = cam == null || !cam.enabled;
            string status =
                "Player Walker (blockout playtest)\n" +
                "Look: hold RIGHT mouse (or click to lock)   Esc = release   WASD = move   Shift = run   Space = jump   E = sit/stand\n" +
                "Mouse locked: " + (Cursor.lockState == CursorLockMode.Locked ? "YES" : "no") +
                "   Grounded: " + (seat != null ? "seated" : (cc != null && cc.isGrounded ? "yes" : "NO")) +
                "   Eye height: " + (eye != null ? eye.position.y.ToString("F2") : "-") + " m" +
                "   Pos: " + transform.position.ToString("F1") +
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
                "   Input: new Input System";
#else
                "   Input: legacy Input Manager";
#endif
            if (camOff)
                status += "\n!! My camera was switched off by another camera script (FlyCamera?). Untick FlyCamera in the Hierarchy.";
            float h = camOff ? 92f : 70f;
            GUI.Box(new Rect(12f, Screen.height - h - 12f, 760f, h), status);
        }

        // ------------------------------------------------------------------ input
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static Vector2 MoveInput()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
        static Vector2 LookDelta()
        {
            Mouse m = Mouse.current;
            return m == null ? Vector2.zero : m.delta.ReadValue() * 0.05f;
        }
        static bool JumpPressed() { Keyboard k = Keyboard.current; return k != null && k.spaceKey.wasPressedThisFrame; }
        static bool InteractPressed() { Keyboard k = Keyboard.current; return k != null && k.eKey.wasPressedThisFrame; }
        static bool RunHeld() { Keyboard k = Keyboard.current; return k != null && k.leftShiftKey.isPressed; }
        static bool EscPressed() { Keyboard k = Keyboard.current; return k != null && k.escapeKey.wasPressedThisFrame; }
        static bool ClickPressed() { Mouse m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; }
        static bool RightHeld() { Mouse m = Mouse.current; return m != null && m.rightButton.isPressed; }
#else
        static Vector2 MoveInput()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
        }
        static Vector2 LookDelta() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        static bool JumpPressed() { return Input.GetKeyDown(KeyCode.Space); }
        static bool InteractPressed() { return Input.GetKeyDown(KeyCode.E); }
        static bool RunHeld() { return Input.GetKey(KeyCode.LeftShift); }
        static bool EscPressed() { return Input.GetKeyDown(KeyCode.Escape); }
        static bool ClickPressed() { return Input.GetMouseButtonDown(0); }
        static bool RightHeld() { return Input.GetMouseButton(1); }
#endif
    }
}

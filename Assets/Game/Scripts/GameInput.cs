using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace PhoneGame
{
    /// <summary>One place for all input, so it works with the old Input Manager or the new Input System.</summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        static Keyboard K { get { return Keyboard.current; } }
        static Mouse Ms { get { return Mouse.current; } }
        static bool Down(KeyControl k) { return k != null && k.wasPressedThisFrame; }
        static bool Held(KeyControl k) { return k != null && k.isPressed; }

        public static Vector2 MouseDelta() { return Ms == null ? Vector2.zero : Ms.delta.ReadValue() * 0.05f; }
        public static bool RightHeld() { return Ms != null && Ms.rightButton.isPressed; }
        public static bool LeftHeld() { return Ms != null && Ms.leftButton.isPressed; }
        public static bool LeftDown() { return Ms != null && Ms.leftButton.wasPressedThisFrame; }
        public static float Scroll() { return Ms == null ? 0f : Mathf.Clamp(Ms.scroll.ReadValue().y, -1f, 1f); }
        public static Vector2 Move()
        {
            if (K == null) return Vector2.zero;
            float x = (Held(K.dKey) ? 1f : 0f) - (Held(K.aKey) ? 1f : 0f);
            float y = (Held(K.wKey) ? 1f : 0f) - (Held(K.sKey) ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static bool InteractDown() { return K != null && Down(K.eKey); }
        public static bool BackDown() { return K != null && Down(K.qKey); }
        public static bool SpaceDown() { return K != null && Down(K.spaceKey); }
        public static bool SpaceHeld() { return K != null && Held(K.spaceKey); }
        public static bool AnyKeyDown() { return (K != null && K.anyKey.wasPressedThisFrame) || LeftDown(); }
        public static bool LeftKeyDown() { return K != null && (Down(K.aKey) || Down(K.leftArrowKey)); }
        public static bool RightKeyDown() { return K != null && (Down(K.dKey) || Down(K.rightArrowKey)); }
        public static bool LeftKeyHeld() { return K != null && (Held(K.aKey) || Held(K.leftArrowKey)); }
        public static bool RightKeyHeld() { return K != null && (Held(K.dKey) || Held(K.rightArrowKey)); }
        public static bool BackspaceDown() { return K != null && Down(K.backspaceKey); }
        public static bool EnterDown() { return K != null && (Down(K.enterKey) || Down(K.numpadEnterKey)); }
        public static bool EscDown() { return K != null && Down(K.escapeKey); }
        public static bool RunHeld() { return K != null && Held(K.leftShiftKey); }
        /// <summary>Walk quietly (half speed): Left Ctrl or C.</summary>
        public static bool SneakHeld() { return K != null && (Held(K.leftCtrlKey) || Held(K.cKey)); }
        /// <summary>The phone's silent mode switch (Chapter 5): M.</summary>
        public static bool MuteDown() { return K != null && Down(K.mKey); }
        /// <summary>2x story speed toggle: Tab.</summary>
        public static bool SpeedDown() { return K != null && Down(K.tabKey); }
        /// <summary>Playtest shortcut: skip the current chapter.</summary>
        public static bool SkipDown() { return K != null && Down(K.f8Key); }

        /// <summary>Digit pressed this frame (top row or numpad), or -1.</summary>
        public static int DigitDown()
        {
            if (K == null) return -1;
            KeyControl[] top = { K.digit0Key, K.digit1Key, K.digit2Key, K.digit3Key, K.digit4Key, K.digit5Key, K.digit6Key, K.digit7Key, K.digit8Key, K.digit9Key };
            KeyControl[] pad = { K.numpad0Key, K.numpad1Key, K.numpad2Key, K.numpad3Key, K.numpad4Key, K.numpad5Key, K.numpad6Key, K.numpad7Key, K.numpad8Key, K.numpad9Key };
            for (int i = 0; i < 10; i++) if (Down(top[i]) || Down(pad[i])) return i;
            return -1;
        }
#else
        public static Vector2 MouseDelta() { return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")); }
        public static bool RightHeld() { return Input.GetMouseButton(1); }
        public static bool LeftHeld() { return Input.GetMouseButton(0); }
        public static bool LeftDown() { return Input.GetMouseButtonDown(0); }
        public static float Scroll() { return Input.mouseScrollDelta.y; }
        public static Vector2 Move()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            return new Vector2(x, y);
        }
        public static bool InteractDown() { return Input.GetKeyDown(KeyCode.E); }
        public static bool BackDown() { return Input.GetKeyDown(KeyCode.Q); }
        public static bool SpaceDown() { return Input.GetKeyDown(KeyCode.Space); }
        public static bool SpaceHeld() { return Input.GetKey(KeyCode.Space); }
        public static bool AnyKeyDown() { return Input.anyKeyDown; }
        public static bool LeftKeyDown() { return Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow); }
        public static bool RightKeyDown() { return Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow); }
        public static bool LeftKeyHeld() { return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow); }
        public static bool RightKeyHeld() { return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow); }
        public static bool BackspaceDown() { return Input.GetKeyDown(KeyCode.Backspace); }
        public static bool EnterDown() { return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter); }
        public static bool EscDown() { return Input.GetKeyDown(KeyCode.Escape); }
        public static bool RunHeld() { return Input.GetKey(KeyCode.LeftShift); }
        public static bool SneakHeld() { return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C); }
        public static bool MuteDown() { return Input.GetKeyDown(KeyCode.M); }
        public static bool SpeedDown() { return Input.GetKeyDown(KeyCode.Tab); }
        /// <summary>Playtest shortcut: skip the current chapter.</summary>
        public static bool SkipDown() { return Input.GetKeyDown(KeyCode.F8); }

        /// <summary>Digit pressed this frame (top row or numpad), or -1.</summary>
        public static int DigitDown()
        {
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) return i;
            }
            return -1;
        }
#endif
    }
}

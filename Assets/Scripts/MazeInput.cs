using UnityEngine;
//#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
//#endif

namespace com.albol.TiltingMaze{
    /// <summary>
    /// Keyboard input that works with either the old Input Manager or the new Input System
    /// (whichever the project has enabled under Player Settings > Active Input Handling).
    /// </summary>
    public static class MazeInput
    {
        /// <summary>x: right arrow = +1, left = -1. y: up arrow = +1, down = -1.</summary>
        public static Vector2 Tilt()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.rightArrowKey.isPressed || k.dKey.isPressed ? 1f : 0f) -
                      (k.leftArrowKey.isPressed || k.aKey.isPressed ? 1f : 0f);
            float y = (k.upArrowKey.isPressed || k.wKey.isPressed ? 1f : 0f) -
                      (k.downArrowKey.isPressed || k.sKey.isPressed ? 1f : 0f);
#else
            float x = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1f : 0f) -
                      (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1f : 0f) -
                      (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1f : 0f);
#endif
            return new Vector2(x, y);
        }

        public static bool RestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.rKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
        }

        public static bool NewMazePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.nKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.N);
#endif
        }

        public static bool QuitPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    
        public static void OnTilt(InputAction.CallbackContext context){
        // Read the Vector3 value from the accelerometer
        Vector3 rawAcceleration = context.ReadValue<Vector3>();

        Vector3 movementVector = new Vector3(rawAcceleration.x, 0, rawAcceleration.y);   
    }
}}


using UnityEngine;

namespace com.albol.TiltingMaze{
    /// <summary>
    /// The tilting part of the labyrinth. It is a kinematic Rigidbody rotated with
    /// MoveRotation, so the marble (a normal dynamic Rigidbody) gets pushed and rolls
    /// correctly under gravity while the board moves.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MazeBoard : MonoBehaviour{
        [Tooltip("Largest tilt in degrees, in each direction.")]
        public float maxTilt = 10f;
        [Tooltip("How fast the board tilts while an arrow key is held (degrees per second).")]
        public float tiltSpeed = 40f;
        [Tooltip("How fast the board returns to level when no key is held (degrees per second).")]
        public float levelSpeed = 25f;
        [Tooltip("On: the board eases back to level when you let go. Off: it stays where you leave it, like the real knobs.")]
        public bool autoLevel = true;

        /// <summary>Set every frame by the game: arrow key state, -1..1 on each axis.</summary>
        [System.NonSerialized] public Vector2 input;
        /// <summary>When true, input is ignored and the board goes back to level.</summary>
        [System.NonSerialized] public bool forceLevel;

        /// <summary>Current tilt in degrees. x &gt; 0: right side down. y &gt; 0: far side down.</summary>
        public Vector2 Tilt { get; private set; }

        Rigidbody _body;
        Quaternion _baseRotation;

        void Awake(){
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = true;
            _body.useGravity = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _baseRotation = transform.rotation;
        }

        public void ResetTilt(){
            Tilt = Vector2.zero;
            input = Vector2.zero;
            transform.rotation = _baseRotation;
            _body.rotation = _baseRotation;
        }

        void FixedUpdate(){
            float dt = Time.fixedDeltaTime;
            Vector2 target = forceLevel ? Vector2.zero : input;
            Tilt = new Vector2(Step(Tilt.x, target.x, dt), Step(Tilt.y, target.y, dt));
            // Pitch around X lowers the far edge; negative roll around Z lowers the right edge.
            _body.MoveRotation(_baseRotation * Quaternion.Euler(Tilt.y, 0f, -Tilt.x));
        }

        float Step(float current, float key, float dt){
            if (Mathf.Abs(key) > 0.01f)
                return Mathf.MoveTowards(current, key * maxTilt, tiltSpeed * dt);
            if (autoLevel || forceLevel)
                return Mathf.MoveTowards(current, 0f, levelSpeed * dt);
            return current;
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Moves an object between its start position and start + Offset (the yellow line in the Scene view).
    /// For moving platforms, lifts, sliding doors, rising lava, spinning saws...
    /// Players standing on it ride along.
    /// </summary>
    public class Mover : MonoBehaviour
    {
        [Tooltip("Where to move, measured from the start position")]
        public Vector2 Offset = new Vector2(0f, 3f);
        [Tooltip("Units per second")]
        [Min(0f)] public float Speed = 2f;
        [Tooltip("On = moves back and forth by itself. Off = only moves when an event tells it to")]
        public bool MoveAutomatically = true;
        [Tooltip("Seconds to wait at each end")]
        [Min(0f)] public float WaitTime = 0.5f;
        [Tooltip("Degrees per second, e.g. for saws. 0 = no rotation")]
        public float RotationSpeed = 0f;

        [Header("Events")]
        public UnityEvent OnReachedEnd = new UnityEvent();
        public UnityEvent OnReachedStart = new UnityEvent();

        Rigidbody2D body;
        Vector2 start;
        bool towardsEnd = true;
        bool moving;
        bool atEnd;
        float waitUntil;

        void Awake()
        {
            this.body = GetComponent<Rigidbody2D>();
            if (this.body == null)
                this.body = gameObject.AddComponent<Rigidbody2D>();

            this.body.bodyType = RigidbodyType2D.Kinematic;
            this.body.interpolation = RigidbodyInterpolation2D.Interpolate;
            this.start = this.body.position;
            this.moving = MoveAutomatically;
        }

        void FixedUpdate()
        {
            if (RotationSpeed != 0f)
                this.body.MoveRotation(this.body.rotation + RotationSpeed * Time.fixedDeltaTime);

            if (!this.moving || Time.time < this.waitUntil)
                return;

            Vector2 target = this.towardsEnd ? this.start + Offset : this.start;
            Vector2 next = Vector2.MoveTowards(this.body.position, target, Speed * Time.fixedDeltaTime);
            this.body.MovePosition(next);

            if ((next - target).sqrMagnitude > 0.00001f)
                return;

            this.atEnd = this.towardsEnd;
            if (this.towardsEnd)
                OnReachedEnd.Invoke();
            else
                OnReachedStart.Invoke();

            if (MoveAutomatically)
            {
                this.towardsEnd = !this.towardsEnd;
                this.waitUntil = Time.time + WaitTime;
            }
            else
            {
                this.moving = false;
            }
        }

        public void MoveToEnd()
        {
            this.towardsEnd = true;
            this.moving = true;
        }

        public void MoveToStart()
        {
            this.towardsEnd = false;
            this.moving = true;
        }

        /// <summary>Moves to the other end.</summary>
        public void Toggle()
        {
            bool goingToEnd = this.moving ? this.towardsEnd : this.atEnd;
            if (goingToEnd)
                MoveToStart();
            else
                MoveToEnd();
        }

        public void StartMoving()
        {
            MoveAutomatically = true;
            this.moving = true;
        }

        public void StopMoving()
        {
            MoveAutomatically = false;
            this.moving = false;
        }

        void OnDrawGizmos()
        {
            Vector3 from = Application.isPlaying ? (Vector3)this.start : transform.position;
            Vector3 to = from + (Vector3)Offset;
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 1f);
            Gizmos.DrawLine(from, to);

            var box = GetComponent<Collider2D>();
            Vector3 size = box != null ? box.bounds.size : Vector3.one;
            Gizmos.DrawWireCube(to + (box != null ? box.bounds.center - transform.position : Vector3.zero), size);
        }
    }
}

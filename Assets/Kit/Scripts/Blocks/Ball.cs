using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A ball for goals. Remembers the player who touched or shot it last.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        [Tooltip("Maximum speed of the ball")]
        public float MaxSpeed = 25f;
        [Tooltip("Seconds the ball floats in the air after a reset")]
        [Min(0f)] public float ResetDelay = 1f;
        public AudioClip BounceSound;

        [Header("Events")]
        public UnityEvent OnReset = new UnityEvent();

        /// <summary>Number of the player who touched or shot the ball last (0 = nobody).</summary>
        public int LastTouchedBy { get; set; }
        public bool IsResetting { get; private set; }

        Rigidbody2D body;
        Vector2 startPosition;

        void Awake()
        {
            this.body = GetComponent<Rigidbody2D>();
            this.startPosition = this.body.position;
        }

        void Start()
        {
            // Float until the round starts, then fall.
            StartCoroutine(Hold(ResetDelay));
        }

        void FixedUpdate()
        {
            if (this.body.linearVelocity.sqrMagnitude > MaxSpeed * MaxSpeed)
                this.body.linearVelocity = this.body.linearVelocity.normalized * MaxSpeed;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            var character = collision.collider.GetComponentInParent<Character>();
            if (character != null)
                LastTouchedBy = character.PlayerNumber;

            if (collision.relativeVelocity.sqrMagnitude > 4f)
                GameManager.PlaySound(BounceSound, 0.4f);
        }

        /// <summary>Puts the ball back to where it started.</summary>
        public void ResetBall()
        {
            ResetBall(0f);
        }

        public void ResetBall(float delay)
        {
            if (!IsResetting && isActiveAndEnabled)
                StartCoroutine(ResetRoutine(delay));
        }

        IEnumerator ResetRoutine(float delay)
        {
            IsResetting = true;
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            this.body.position = this.startPosition;
            transform.position = new Vector3(this.startPosition.x, this.startPosition.y, transform.position.z);
            this.body.linearVelocity = Vector2.zero;
            this.body.angularVelocity = 0f;
            LastTouchedBy = 0;
            OnReset.Invoke();

            yield return Hold(ResetDelay);
            IsResetting = false;
        }

        IEnumerator Hold(float seconds)
        {
            this.body.bodyType = RigidbodyType2D.Kinematic;
            this.body.linearVelocity = Vector2.zero;
            this.body.angularVelocity = 0f;

            while (LevelRules.Current != null && !LevelRules.Current.IsRunning)
                yield return null;
            yield return new WaitForSeconds(seconds);

            this.body.bodyType = RigidbodyType2D.Dynamic;
        }
    }
}

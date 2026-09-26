using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Launches players, balls, grenades... in the direction of its green arrow (rotate it for side boosters).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class JumpPad : MonoBehaviour
    {
        [Tooltip("How fast things get launched")]
        public float LaunchSpeed = 18f;
        [Tooltip("Also launch balls, crates and projectiles")]
        public bool LaunchObjects = true;

        [Header("Look")]
        public SpriteRenderer Renderer;
        public Sprite IdleSprite;
        public Sprite LaunchSprite;
        public AudioClip Sound;

        [Header("Events")]
        [Tooltip("Called with the player who got launched (0 = no player)")]
        public UnityEvent<int> OnLaunch = new UnityEvent<int>();

        const float CooldownPerObject = 0.2f;
        readonly Dictionary<Rigidbody2D, float> lastLaunch = new Dictionary<Rigidbody2D, float>();

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Launch(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            Launch(other);
        }

        void Launch(Collider2D other)
        {
            Rigidbody2D target = other.attachedRigidbody;
            if (target == null || target.bodyType != RigidbodyType2D.Dynamic)
                return;

            var character = target.GetComponent<Character>();
            if (character == null && !LaunchObjects)
                return;
            if (character != null && character.IsDead)
                return;

            if (this.lastLaunch.TryGetValue(target, out float last) && Time.time < last + CooldownPerObject)
                return;
            this.lastLaunch[target] = Time.time;

            // Replace the speed along the launch direction, keep the sideways speed.
            Vector2 up = transform.up;
            Vector2 velocity = target.linearVelocity;
            velocity = velocity - Vector2.Dot(velocity, up) * up + up * LaunchSpeed;
            target.linearVelocity = velocity;

            GameManager.PlaySound(Sound, 0.8f);
            OnLaunch.Invoke(character != null ? character.PlayerNumber : 0);

            if (Renderer != null && LaunchSprite != null)
            {
                StopAllCoroutines();
                StartCoroutine(ShowLaunchSprite());
            }
        }

        IEnumerator ShowLaunchSprite()
        {
            Renderer.sprite = LaunchSprite;
            yield return new WaitForSeconds(0.25f);
            if (IdleSprite != null)
                Renderer.sprite = IdleSprite;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 from = transform.position;
            Vector3 to = from + transform.up * 1.5f;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawLine(to, to - transform.up * 0.4f + transform.right * 0.3f);
            Gizmos.DrawLine(to, to - transform.up * 0.4f - transform.right * 0.3f);
        }
    }
}

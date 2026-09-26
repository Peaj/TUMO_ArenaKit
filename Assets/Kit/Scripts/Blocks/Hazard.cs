using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Spikes, saws, lava, the pit below the level: hurts or kills players who touch it.
    /// Works with solid colliders and with trigger colliders (e.g. a lava tilemap).
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [Tooltip("Kills players instantly")]
        public bool InstantKill = true;
        [Tooltip("Damage per touch when it does not kill instantly")]
        public float Damage = 34f;
        [Tooltip("Seconds between two hits while touching it")]
        [Min(0.05f)] public float DamageInterval = 0.5f;
        [Tooltip("Bounces players away")]
        public float Knockback = 0f;
        [Tooltip("Also removes other objects (crates, dropped weapons...). Balls go back to their start")]
        public bool RemoveObjects = false;
        public AudioClip HitSound;

        [Header("Events")]
        [Tooltip("Called with the player who got hurt")]
        public UnityEvent<int> OnHurtPlayer = new UnityEvent<int>();

        const float KillDamage = 100000f;
        readonly Dictionary<Object, float> lastHit = new Dictionary<Object, float>();

        void OnTriggerEnter2D(Collider2D other)
        {
            Touch(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            Touch(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Touch(collision.collider);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            Touch(collision.collider);
        }

        void Touch(Collider2D other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character != null)
            {
                HurtPlayer(character);
                return;
            }

            var ball = other.GetComponentInParent<Ball>();
            if (ball != null)
            {
                if (RemoveObjects)
                    ball.ResetBall();
                return;
            }

            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null && IsReady((Object)damageable))
                damageable.TakeDamage(InstantKill ? KillDamage : Damage, 0);

            if (RemoveObjects)
            {
                Rigidbody2D body = other.attachedRigidbody;
                if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                    Destroy(body.gameObject);
            }
        }

        void HurtPlayer(Character character)
        {
            if (character.IsDead || character.IsProtected || !IsReady(character))
                return;

            character.TakeDamage(InstantKill ? KillDamage : Damage, 0);

            if (Knockback > 0f && !character.IsDead)
            {
                Vector2 away = (character.Body.position - (Vector2)transform.position).normalized + Vector2.up;
                character.Body.linearVelocity = Vector2.zero;
                character.Body.AddForce(away.normalized * Knockback, ForceMode2D.Impulse);
            }

            GameManager.PlaySound(HitSound);
            OnHurtPlayer.Invoke(character.PlayerNumber);
        }

        bool IsReady(Object target)
        {
            if (this.lastHit.TryGetValue(target, out float last) && Time.time < last + DamageInterval)
                return false;
            this.lastHit[target] = Time.time;
            return true;
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Something a weapon shoots: bullets, rockets, grenades...
    /// With an explosion radius it hurts and pushes everything around it, otherwise only what it hits.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Header("Damage")]
        [Tooltip("Damage it does. Players have 100 health")]
        public float Damage = 100f;
        [Tooltip("0 = only hits what it touches. More = explodes and hits everything in this radius")]
        [Min(0f)] public float ExplosionRadius = 0f;
        [Tooltip("How hard things get pushed away")]
        public float PushForce = 10f;

        [Header("Flight")]
        [Tooltip("0 = flies straight, 1 = falls like a stone")]
        public float Gravity = 0f;
        [Tooltip("Seconds until it disappears (or explodes, if it has an explosion radius)")]
        [Min(0.05f)] public float Lifetime = 3f;
        [Tooltip("Explode on the first touch. Off = bounces around until the lifetime is over (grenade)")]
        public bool ExplodeOnImpact = true;
        [Tooltip("Turn in the direction it flies (rockets)")]
        public bool FaceFlightDirection = true;

        [Header("Effects")]
        public GameObject ExplosionEffect;
        public AudioClip ExplosionSound;

        [Header("Events")]
        public UnityEvent OnExplode = new UnityEvent();

        /// <summary>Number of the player who fired it (0 = nobody).</summary>
        public int Owner { get; private set; }

        Rigidbody2D body;
        Character ownerCharacter;
        float spawnTime;
        bool exploded;

        void Awake()
        {
            this.body = GetComponent<Rigidbody2D>();
            this.body.gravityScale = Gravity;
            this.spawnTime = Time.time;
        }

        public void Launch(Character owner, Vector2 velocity)
        {
            this.ownerCharacter = owner;
            Owner = owner != null ? owner.PlayerNumber : 0;
            this.body.gravityScale = Gravity;
            this.body.linearVelocity = velocity;

            // Never collide with the shooter (grenades would bounce off them).
            if (owner != null)
                foreach (Collider2D own in GetComponentsInChildren<Collider2D>())
                    foreach (Collider2D other in owner.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(own, other, true);
        }

        void Update()
        {
            if (this.exploded || Time.time - this.spawnTime < Lifetime)
                return;

            if (ExplosionRadius > 0f)
                Explode(null);
            else
                Destroy(gameObject);
        }

        void FixedUpdate()
        {
            Vector2 velocity = this.body.linearVelocity;
            if (FaceFlightDirection && velocity.sqrMagnitude > 0.1f)
                this.body.MoveRotation(Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            HandleHit(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (ExplodeOnImpact)
                HandleHit(collision.collider);
        }

        void HandleHit(Collider2D other)
        {
            if (this.exploded)
                return;

            // Never hit the shooter directly or other projectiles.
            if (this.ownerCharacter != null && other.attachedRigidbody == this.ownerCharacter.Body)
                return;
            if (other.GetComponentInParent<Projectile>() != null)
                return;

            var shootable = other.GetComponentInParent<IShootable>();

            // Invisible zones don't stop projectiles, but switches can be shot.
            if (other.isTrigger)
            {
                if (shootable == null)
                    return;
                shootable.OnShot(this);
                if (ExplodeOnImpact)
                    Explode(other);
                return;
            }

            if (shootable != null)
                shootable.OnShot(this);

            if (ExplodeOnImpact)
                Explode(other);
        }

        /// <summary>Explodes right now (for events).</summary>
        public void Explode()
        {
            Explode(null);
        }

        void Explode(Collider2D directHit)
        {
            if (this.exploded)
                return;
            this.exploded = true;

            Vector2 position = this.body.position;
            if (ExplosionRadius > 0f)
            {
                Explosion.Create(position, ExplosionRadius, Damage, PushForce, Owner, ExplosionEffect, ExplosionSound);
            }
            else
            {
                if (directHit != null)
                    HitDirectly(directHit);
                if (ExplosionEffect != null)
                    Instantiate(ExplosionEffect, position, Quaternion.identity);
                GameManager.PlaySound(ExplosionSound);
            }

            OnExplode.Invoke();
            Destroy(gameObject);
        }

        void HitDirectly(Collider2D target)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(Damage, Owner);

            Rigidbody2D targetBody = target.attachedRigidbody;
            if (targetBody != null && targetBody.bodyType == RigidbodyType2D.Dynamic)
                targetBody.AddForce(this.body.linearVelocity.normalized * PushForce, ForceMode2D.Impulse);

            var ball = target.GetComponentInParent<Ball>();
            if (ball != null && Owner > 0)
                ball.LastTouchedBy = Owner;
        }
    }
}

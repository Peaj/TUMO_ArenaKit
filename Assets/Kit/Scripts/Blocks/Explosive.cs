using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Mines, barrels, bombs: explodes when touched, shot, hit by another explosion or told so by an event.
    /// </summary>
    public class Explosive : MonoBehaviour, IDamageable, IShootable
    {
        [Header("Explosion")]
        public float Damage = 100f;
        [Min(0.1f)] public float Radius = 2.5f;
        [Tooltip("How hard things get pushed away")]
        public float PushForce = 16f;

        [Header("Trigger")]
        [Tooltip("Explodes when a player touches it (mine)")]
        public bool ExplodeOnTouch = true;
        [Tooltip("Explodes when it gets shot or hit by another explosion (barrel)")]
        public bool ExplodeWhenHit = true;
        [Tooltip("Seconds between the trigger and the boom")]
        [Min(0f)] public float FuseTime = 0.4f;
        [Tooltip("Comes back after these seconds. 0 = gone for good")]
        [Min(0f)] public float RespawnTime = 0f;

        [Header("Look and sound")]
        public SpriteRenderer Renderer;
        [Tooltip("Sprite while the fuse burns")]
        public Sprite ArmedSprite;
        public GameObject ExplosionEffect;
        public AudioClip FuseSound;
        public AudioClip ExplosionSound;

        [Header("Events")]
        public UnityEvent OnExplode = new UnityEvent();

        bool armed;
        int triggeredBy;
        Sprite idleSprite;
        Collider2D[] colliders;
        Rigidbody2D body;
        Vector3 startPosition;
        Quaternion startRotation;

        void Awake()
        {
            this.colliders = GetComponents<Collider2D>();
            this.body = GetComponent<Rigidbody2D>();
            this.startPosition = transform.position;
            this.startRotation = transform.rotation;
            if (Renderer != null)
                this.idleSprite = Renderer.sprite;
        }

        /// <summary>Lights the fuse (for events).</summary>
        public void Explode()
        {
            Arm(0);
        }

        public void TakeDamage(float damage, int attacker)
        {
            if (ExplodeWhenHit)
                Arm(attacker);
        }

        public void OnShot(Projectile projectile)
        {
            if (ExplodeWhenHit)
                Arm(projectile.Owner);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            Touch(other);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            Touch(collision.collider);
        }

        void Touch(Collider2D other)
        {
            if (!ExplodeOnTouch)
                return;
            var character = other.GetComponentInParent<Character>();
            if (character != null && !character.IsDead)
                Arm(0);
        }

        void Arm(int attacker)
        {
            if (this.armed || !isActiveAndEnabled)
                return;

            this.armed = true;
            this.triggeredBy = attacker;
            StartCoroutine(Fuse());
        }

        IEnumerator Fuse()
        {
            if (Renderer != null && ArmedSprite != null)
                Renderer.sprite = ArmedSprite;
            GameManager.PlaySound(FuseSound, 0.6f);

            float end = Time.time + Mathf.Max(0.05f, FuseTime);
            while (Time.time < end)
            {
                if (Renderer != null)
                    Renderer.color = Mathf.Repeat(Time.time * 10f, 1f) > 0.5f ? new Color(1f, 0.5f, 0.5f) : Color.white;
                yield return null;
            }

            Boom();
        }

        void Boom()
        {
            Vector2 position = transform.position;
            SetVisible(false);
            Explosion.Create(position, Radius, Damage, PushForce, this.triggeredBy, ExplosionEffect, ExplosionSound);
            OnExplode.Invoke();

            if (RespawnTime > 0f)
                StartCoroutine(Respawn());
            else
                Destroy(gameObject);
        }

        IEnumerator Respawn()
        {
            yield return new WaitForSeconds(RespawnTime);
            transform.SetPositionAndRotation(this.startPosition, this.startRotation);
            if (this.body != null)
            {
                this.body.position = this.startPosition;
                this.body.linearVelocity = Vector2.zero;
                this.body.angularVelocity = 0f;
            }
            if (Renderer != null)
                Renderer.sprite = this.idleSprite;
            SetVisible(true);
            this.armed = false;
        }

        void SetVisible(bool visible)
        {
            foreach (Collider2D own in this.colliders)
                own.enabled = visible;
            if (Renderer != null)
            {
                Renderer.enabled = visible;
                Renderer.color = Color.white;
            }
            if (this.body != null)
                this.body.simulated = visible;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}

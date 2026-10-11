using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Blows this object up: lights the fuse, then hurts and pushes away everything in the radius.
    /// Call "Explode" from any event: a TriggerZone (mine), Breakable > On Broken (TNT), a Timer (time bomb)...
    /// The player who triggered it gets the kill points.
    /// </summary>
    public class Explosive : MonoBehaviour
    {
        [Header("Explosion")]
        public float Damage = 100f;
        [Min(0.1f)] public float Radius = 2.5f;
        [Tooltip("How hard things get pushed away")]
        public float PushForce = 16f;
        [Tooltip("Seconds between the trigger and the boom")]
        [Min(0f)] public float FuseTime = 0.4f;

        [Header("After the boom")]
        [Tooltip("On = the object is gone after the explosion")]
        public bool RemoveObject = true;
        [Tooltip("Comes back after these seconds. 0 = gone for good")]
        [Min(0f)] public float RespawnTime = 0f;

        [Header("Look and sound")]
        [Tooltip("Blinks while the fuse burns")]
        public SpriteRenderer Renderer;
        public GameObject ExplosionEffect;
        public AudioClip FuseSound;
        public AudioClip ExplosionSound;

        [Header("Events")]
        public UnityEvent OnExplode = new UnityEvent();

        public bool IsArmed { get; private set; }

        int triggeredBy;
        Collider2D[] colliders;
        SpriteRenderer[] renderers;
        Rigidbody2D body;
        Vector3 startPosition;
        Quaternion startRotation;

        void Awake()
        {
            this.colliders = GetComponentsInChildren<Collider2D>();
            this.renderers = GetComponentsInChildren<SpriteRenderer>();
            this.body = GetComponent<Rigidbody2D>();
            this.startPosition = transform.position;
            this.startRotation = transform.rotation;
        }

        /// <summary>Lights the fuse. Does nothing if it is already burning.</summary>
        public void Explode()
        {
            if (IsArmed || !isActiveAndEnabled)
                return;

            IsArmed = true;
            this.triggeredBy = TriggeredBy.Player;
            StartCoroutine(Fuse());
        }

        IEnumerator Fuse()
        {
            GameManager.PlaySound(FuseSound, 0.6f);

            float end = Time.time + FuseTime;
            while (Time.time < end)
            {
                if (Renderer != null)
                    Renderer.color = Mathf.Repeat(Time.time * 10f, 1f) > 0.5f ? new Color(1f, 0.5f, 0.5f) : Color.white;
                yield return null;
            }
            if (Renderer != null)
                Renderer.color = Color.white;

            Boom();
        }

        void Boom()
        {
            if (RemoveObject)
                SetVisible(false);

            Explosion.Create(transform.position, Radius, Damage, PushForce, this.triggeredBy, ExplosionEffect, ExplosionSound);
            TriggeredBy.Invoke(OnExplode, TriggeredBy.PlayerObject(this.triggeredBy), this.triggeredBy);

            if (!RemoveObject)
                IsArmed = false;
            else if (RespawnTime > 0f)
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
            SetVisible(true);
            IsArmed = false;
        }

        void SetVisible(bool visible)
        {
            foreach (Collider2D own in this.colliders)
                own.enabled = visible;
            foreach (SpriteRenderer spriteRenderer in this.renderers)
                spriteRenderer.enabled = visible;
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

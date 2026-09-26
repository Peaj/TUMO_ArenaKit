using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Crates and walls that break when they take enough damage. Can drop something, e.g. a weapon.
    /// </summary>
    public class Breakable : MonoBehaviour, IDamageable
    {
        [Tooltip("Damage it can take before it breaks")]
        public float Health = 50f;
        [Tooltip("One of these appears when it breaks (e.g. weapon pickups). Empty = nothing")]
        public GameObject[] Drops;
        [Tooltip("Pieces flying around when it breaks")]
        [Range(0, 12)] public int DebrisPieces = 4;
        public AudioClip HitSound;
        public AudioClip BreakSound;

        [Header("Events")]
        [Tooltip("Called with the player who broke it (0 = no player)")]
        public UnityEvent<int> OnBroken = new UnityEvent<int>();

        bool broken;
        bool flashing;

        public void TakeDamage(float damage, int attacker)
        {
            if (this.broken)
                return;

            Health -= damage;
            if (Health <= 0f)
            {
                Break(attacker);
                return;
            }

            GameManager.PlaySound(HitSound, 0.6f);
            if (!this.flashing && isActiveAndEnabled)
                StartCoroutine(Flash());
        }

        /// <summary>Breaks it right away (for events).</summary>
        public void Break()
        {
            Break(0);
        }

        void Break(int attacker)
        {
            if (this.broken)
                return;
            this.broken = true;

            Vector3 position = transform.position;
            if (Drops != null && Drops.Length > 0)
            {
                GameObject drop = Drops[Random.Range(0, Drops.Length)];
                if (drop != null)
                    Instantiate(drop, new Vector3(position.x, position.y, 0f), Quaternion.identity);
            }

            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                Debris.Burst(spriteRenderer, DebrisPieces);

            GameManager.PlaySound(BreakSound);
            OnBroken.Invoke(attacker);
            Destroy(gameObject);
        }

        IEnumerator Flash()
        {
            this.flashing = true;
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            foreach (SpriteRenderer spriteRenderer in renderers)
                spriteRenderer.color = new Color(1f, 0.6f, 0.6f);
            yield return new WaitForSeconds(0.08f);
            foreach (SpriteRenderer spriteRenderer in renderers)
                if (spriteRenderer != null)
                    spriteRenderer.color = Color.white;
            this.flashing = false;
        }
    }
}

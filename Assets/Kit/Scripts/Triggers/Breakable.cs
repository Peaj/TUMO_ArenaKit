using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Has health and breaks when it took enough damage from shots, explosions or hazards.
    /// Connect "On Broken" to make things happen: a Spawner drops a weapon (weapon crate), Explode blows it up (TNT)...
    /// </summary>
    public class Breakable : MonoBehaviour, IDamageable
    {
        [Tooltip("Damage it can take before it breaks")]
        public float Health = 50f;
        [Tooltip("On = it falls apart when broken. Off = it stays, e.g. so an Explode action can blow it up")]
        public bool RemoveWhenBroken = true;
        [Tooltip("Pieces flying around when it falls apart")]
        [Range(0, 12)] public int DebrisPieces = 4;
        public AudioClip HitSound;
        public AudioClip BreakSound;

        [Header("Events")]
        [Tooltip("Called with the player who did the damage (0 = no player)")]
        public UnityEvent<int> OnDamaged = new UnityEvent<int>();
        [Tooltip("Called with the player who broke it (0 = no player)")]
        public UnityEvent<int> OnBroken = new UnityEvent<int>();

        public bool IsBroken { get; private set; }

        bool flashing;

        public void TakeDamage(float damage, int attacker)
        {
            if (IsBroken)
                return;

            Health -= damage;
            if (Health <= 0f)
            {
                Break(attacker);
                return;
            }

            GameManager.PlaySound(HitSound, 0.6f);
            TriggeredBy.Invoke(OnDamaged, TriggeredBy.PlayerObject(attacker), attacker);
            if (!this.flashing && isActiveAndEnabled)
                StartCoroutine(Flash());
        }

        /// <summary>Breaks it right away (for events).</summary>
        public void Break()
        {
            Break(TriggeredBy.Player);
        }

        void Break(int attacker)
        {
            if (IsBroken)
                return;
            IsBroken = true;

            GameManager.PlaySound(BreakSound);
            TriggeredBy.Invoke(OnBroken, TriggeredBy.PlayerObject(attacker), attacker);

            if (!RemoveWhenBroken)
                return;

            var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                Debris.Burst(spriteRenderer, DebrisPieces);
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

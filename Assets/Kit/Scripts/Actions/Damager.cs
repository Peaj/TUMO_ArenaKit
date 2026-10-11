using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Hurts whatever triggered it: spikes, saws, lava, the pit below the level...
    /// Also breaks crates and other breakable things. Use it with a TriggerZone: On Enter > Damager > Hurt.
    /// For lava that keeps hurting, set "Repeat While Inside" on the TriggerZone.
    /// </summary>
    public class Damager : MonoBehaviour
    {
        [Tooltip("Kills players instantly")]
        public bool InstantKill = true;
        [Tooltip("Damage per hit when it does not kill instantly. Players have 100 health")]
        public float Damage = 34f;
        [Tooltip("Bounces players away")]
        public float Knockback = 0f;
        public AudioClip HitSound;

        const float KillDamage = 100000f;

        /// <summary>Hurts whatever triggered the event.</summary>
        public void Hurt()
        {
            Hit(InstantKill ? KillDamage : Damage);
        }

        /// <summary>Kills whatever triggered the event, even if "Instant Kill" is off.</summary>
        public void Kill()
        {
            Hit(KillDamage);
        }

        void Hit(float damage)
        {
            GameObject thing = TriggeredBy.Thing;
            if (thing == null)
                return;

            var character = thing.GetComponentInParent<Character>();
            if (character != null)
            {
                HurtPlayer(character, damage);
                return;
            }

            // Lava breaks crates, but the ball and rockets are not hurt.
            var damageable = thing.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage, 0);
        }

        void HurtPlayer(Character character, float damage)
        {
            if (character.IsDead || character.IsProtected)
                return;

            character.TakeDamage(damage, 0);
            if (Knockback > 0f && !character.IsDead)
            {
                Vector2 away = (character.Body.position - (Vector2)transform.position).normalized + Vector2.up;
                character.Body.linearVelocity = Vector2.zero;
                character.Body.AddForce(away.normalized * Knockback, ForceMode2D.Impulse);
            }
            GameManager.PlaySound(HitSound);
        }
    }
}

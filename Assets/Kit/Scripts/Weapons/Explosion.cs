using System.Collections.Generic;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Hurts and pushes away everything in a circle. Used by projectiles and explosives.
    /// </summary>
    public static class Explosion
    {
        /// <summary>Explosion radius the effect prefab was made for. Bigger explosions scale the effect up.</summary>
        const float EffectReferenceRadius = 1.6f;

        /// <summary>A little extra lift makes explosions more fun (rocket jumps!).</summary>
        const float UpwardsLift = 0.35f;

        public static void Create(Vector2 position, float radius, float damage, float pushForce, int attacker,
            GameObject effect = null, AudioClip sound = null)
        {
            var hits = new List<Collider2D>();
            Physics2D.OverlapCircle(position, radius, ContactFilter2D.noFilter, hits);

            var damaged = new HashSet<IDamageable>();
            var pushed = new HashSet<Rigidbody2D>();

            foreach (Collider2D hit in hits)
            {
                if (hit == null)
                    continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && damaged.Add(damageable))
                    damageable.TakeDamage(damage, attacker);

                Rigidbody2D body = hit.attachedRigidbody;
                if (body == null || body.bodyType != RigidbodyType2D.Dynamic || !pushed.Add(body))
                    continue;

                Vector2 away = body.worldCenterOfMass - position;
                float distance = away.magnitude;
                Vector2 direction = distance > 0.05f ? away / distance : Vector2.up;
                direction = (direction + Vector2.up * UpwardsLift).normalized;
                float strength = Mathf.Lerp(1f, 0.4f, Mathf.Clamp01(distance / radius));
                body.AddForce(direction * pushForce * strength, ForceMode2D.Impulse);

                var ball = body.GetComponent<Ball>();
                if (ball != null && attacker > 0)
                    ball.LastTouchedBy = attacker;
            }

            if (effect != null)
            {
                GameObject instance = Object.Instantiate(effect, position, Quaternion.identity);
                instance.transform.localScale *= radius / EffectReferenceRadius;
            }

            GameManager.PlaySound(sound);
        }
    }
}

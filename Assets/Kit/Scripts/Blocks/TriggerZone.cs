using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    public enum TriggerTarget
    {
        Players,
        Projectiles,
        Balls,
        AnyPhysicsObject,
        Everything,
    }

    /// <summary>
    /// An invisible zone that calls events when something enters or leaves it.
    /// Connect it to doors, movers, explosives, the level rules... to build your own mechanics.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TriggerZone : MonoBehaviour
    {
        [Tooltip("What can trigger this zone")]
        public TriggerTarget ReactsTo = TriggerTarget.Players;
        [Tooltip("Only works the first time")]
        public bool OnlyOnce = false;
        [Tooltip("Seconds before it can trigger again")]
        [Min(0f)] public float Cooldown = 0f;

        [Header("Events")]
        [Tooltip("Something entered. The number is the player (1-4), or 0 if it was no player")]
        public UnityEvent<int> OnEnter = new UnityEvent<int>();
        [Tooltip("Something left the zone")]
        public UnityEvent<int> OnExit = new UnityEvent<int>();

        readonly Dictionary<Object, int> inside = new Dictionary<Object, int>();
        float lastTriggered = -999f;
        bool used;

        /// <summary>How many things are inside right now.</summary>
        public int Count
        {
            get
            {
                CleanUp();
                return this.inside.Count;
            }
        }

        public bool IsOccupied => Count > 0;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!TriggerTargets.Matches(other, ReactsTo, out Object key, out int player))
                return;

            this.inside.TryGetValue(key, out int colliders);
            this.inside[key] = colliders + 1;

            // Players have more than one collider: only count them once.
            if (colliders > 0)
                return;

            if (this.used || Time.time < this.lastTriggered + Cooldown)
                return;

            this.lastTriggered = Time.time;
            if (OnlyOnce)
                this.used = true;

            OnEnter.Invoke(player);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!TriggerTargets.Matches(other, ReactsTo, out Object key, out int player, ignoreDead: true))
                return;
            if (!this.inside.TryGetValue(key, out int colliders))
                return;

            if (colliders > 1)
            {
                this.inside[key] = colliders - 1;
                return;
            }

            this.inside.Remove(key);
            OnExit.Invoke(player);
        }

        void CleanUp()
        {
            List<Object> gone = null;
            foreach (Object key in this.inside.Keys)
                if (key == null)
                    (gone ??= new List<Object>()).Add(key);
            if (gone != null)
                foreach (Object key in gone)
                    this.inside.Remove(key);
        }

        void OnDrawGizmos()
        {
            Gizmos2D.DrawCollider(GetComponent<Collider2D>(), new Color(0.2f, 1f, 0.4f, 0.25f));
        }
    }

    /// <summary>Shared "does this collider count?" check for zones, switches, goals...</summary>
    public static class TriggerTargets
    {
        public static bool Matches(Collider2D other, TriggerTarget target, out Object key, out int player, bool ignoreDead = false)
        {
            key = null;
            player = 0;

            var character = other.GetComponentInParent<Character>();
            var projectile = character == null ? other.GetComponentInParent<Projectile>() : null;
            var ball = character == null ? other.GetComponentInParent<Ball>() : null;
            Rigidbody2D body = other.attachedRigidbody;

            if (character != null)
                player = character.PlayerNumber;
            else if (projectile != null)
                player = projectile.Owner;
            else if (ball != null)
                player = ball.LastTouchedBy;

            switch (target)
            {
                case TriggerTarget.Players:
                    if (character == null || (character.IsDead && !ignoreDead))
                        return false;
                    key = character;
                    return true;

                case TriggerTarget.Projectiles:
                    key = projectile;
                    return projectile != null;

                case TriggerTarget.Balls:
                    key = ball;
                    return ball != null;

                case TriggerTarget.AnyPhysicsObject:
                    if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
                        return false;
                    key = body;
                    return true;

                default:
                    key = body != null ? body : other;
                    return true;
            }
        }
    }
}

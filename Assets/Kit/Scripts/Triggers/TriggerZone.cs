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
    /// Calls events when something enters or leaves its collider. The most important trigger of the kit:
    /// jump pads, mines, coins, spikes, teleporters, goals... are all a TriggerZone plus an action.
    /// Actions in its events (ForcePush, Hurt, Score...) work on whatever entered the zone.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class TriggerZone : MonoBehaviour
    {
        [Tooltip("What can trigger this zone")]
        public TriggerTarget ReactsTo = TriggerTarget.Players;
        [Tooltip("0 = every player. 1-4 = only this player (only for 'Players')")]
        [Range(0, GameManager.MaxPlayers)] public int OnlyForPlayer = 0;
        [Tooltip("Only works the first time")]
        public bool OnlyOnce = false;
        [Tooltip("Seconds before it can trigger again")]
        [Min(0f)] public float Cooldown = 0f;
        [Tooltip("While something stays inside, call 'On Enter' again every X seconds (e.g. lava that keeps hurting). 0 = only once")]
        [Min(0f)] public float RepeatWhileInside = 0f;

        [Header("Events")]
        [Tooltip("Something entered. The number is the player (1-4), or 0 if it was no player")]
        public UnityEvent<int> OnEnter = new UnityEvent<int>();
        [Tooltip("Something left the zone")]
        public UnityEvent<int> OnExit = new UnityEvent<int>();

        class Inside
        {
            public int Colliders;
            public GameObject Thing;
            public int Player;
            public float NextRepeat;
        }

        readonly Dictionary<Object, Inside> inside = new Dictionary<Object, Inside>();
        readonly List<Object> keys = new List<Object>();
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
            if (!Matches(other, false, out Object key, out int player))
                return;

            if (this.inside.TryGetValue(key, out Inside entry))
            {
                // Players have more than one collider: only count them once.
                entry.Colliders++;
                return;
            }

            entry = new Inside { Colliders = 1, Thing = ThingOf(key, other), Player = player, NextRepeat = Time.time + RepeatWhileInside };
            this.inside[key] = entry;
            Enter(entry);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!Matches(other, true, out Object key, out int player))
                return;
            if (!this.inside.TryGetValue(key, out Inside entry))
                return;

            if (--entry.Colliders > 0)
                return;

            this.inside.Remove(key);
            TriggeredBy.Invoke(OnExit, entry.Thing, player);
        }

        void Update()
        {
            if (RepeatWhileInside <= 0f || this.inside.Count == 0)
                return;

            CleanUp();
            this.keys.Clear();
            this.keys.AddRange(this.inside.Keys);
            foreach (Object key in this.keys)
            {
                if (!this.inside.TryGetValue(key, out Inside entry) || Time.time < entry.NextRepeat)
                    continue;
                entry.NextRepeat = Time.time + RepeatWhileInside;
                // Dead players stay "inside" until they leave, but should not be hurt again.
                var character = key as Character;
                if (character == null || !character.IsDead)
                    TriggeredBy.Invoke(OnEnter, entry.Thing, entry.Player);
            }
        }

        void Enter(Inside entry)
        {
            if (this.used || Time.time < this.lastTriggered + Cooldown)
                return;

            this.lastTriggered = Time.time;
            if (OnlyOnce)
                this.used = true;

            TriggeredBy.Invoke(OnEnter, entry.Thing, entry.Player);
        }

        bool Matches(Collider2D other, bool ignoreDead, out Object key, out int player)
        {
            if (!TriggerTargets.Matches(other, ReactsTo, out key, out player, ignoreDead))
                return false;
            return ReactsTo != TriggerTarget.Players || OnlyForPlayer == 0 || player == OnlyForPlayer;
        }

        static GameObject ThingOf(Object key, Collider2D other)
        {
            if (key is Component component)
                return component.gameObject;
            return other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        }

        void CleanUp()
        {
            List<Object> gone = null;
            foreach (KeyValuePair<Object, Inside> pair in this.inside)
                if (pair.Key == null || pair.Value.Thing == null)
                    (gone ??= new List<Object>()).Add(pair.Key);
            if (gone != null)
                foreach (Object key in gone)
                    this.inside.Remove(key);
        }

        void OnDrawGizmos()
        {
            Gizmos2D.DrawCollider(GetComponent<Collider2D>(), new Color(0.2f, 1f, 0.4f, 0.25f));
        }
    }

    /// <summary>Shared "does this collider count?" check for zones, score zones...</summary>
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
                    key = character != null ? (Object)character : body;
                    return true;

                default:
                    key = character != null ? (Object)character : body != null ? (Object)body : other;
                    return true;
            }
        }
    }
}

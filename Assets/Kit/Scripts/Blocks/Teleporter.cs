using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Beams players (and objects) to its target teleporter. Speed is kept.
    /// Link two teleporters to each other to go back and forth.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Teleporter : MonoBehaviour
    {
        [Tooltip("Where things come out")]
        public Teleporter Target;
        [Tooltip("Also beam projectiles, balls and crates")]
        public bool TeleportObjects = true;
        public AudioClip Sound;

        [Header("Events")]
        [Tooltip("Called with the player who got teleported (0 = no player)")]
        public UnityEvent<int> OnTeleport = new UnityEvent<int>();

        const float Cooldown = 0.4f;
        static readonly Dictionary<Rigidbody2D, float> lastTeleport = new Dictionary<Rigidbody2D, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lastTeleport.Clear();
        }

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (Target == null || Target == this)
                return;

            Rigidbody2D body = other.attachedRigidbody;
            if (body == null || body.bodyType != RigidbodyType2D.Dynamic)
                return;

            var character = body.GetComponent<Character>();
            if (character == null && !TeleportObjects)
                return;
            if (character != null && character.IsDead)
                return;

            if (lastTeleport.TryGetValue(body, out float last) && Time.time < last + Cooldown)
                return;
            lastTeleport[body] = Time.time;

            Vector2 exit = Target.transform.position;
            if (character != null)
                character.Teleport(exit);
            else
            {
                body.position = exit;
                body.transform.position = new Vector3(exit.x, exit.y, body.transform.position.z);
            }

            GameManager.PlaySound(Sound, 0.7f);
            OnTeleport.Invoke(character != null ? character.PlayerNumber : 0);
        }

        void OnDrawGizmos()
        {
            if (Target == null)
                return;
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position, Target.transform.position);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Beams whatever triggered it to the target. Speed is kept.
    /// The TeleporterPair prefab: two portals, each a TriggerZone > Teleporter > Teleport with the other one as target.
    /// </summary>
    public class Teleporter : MonoBehaviour
    {
        [Tooltip("Where things come out")]
        public Transform Target;
        [Tooltip("Also beam projectiles, balls and crates, not only players")]
        public bool TeleportObjects = true;
        public AudioClip Sound;

        /// <summary>Coming out of a portal must not beam you right back.</summary>
        const float Cooldown = 0.4f;
        static readonly Dictionary<Rigidbody2D, float> lastTeleport = new Dictionary<Rigidbody2D, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            lastTeleport.Clear();
        }

        /// <summary>Beams whatever triggered the event to the target.</summary>
        public void Teleport()
        {
            Rigidbody2D body = TriggeredBy.Body;
            if (Target == null || body == null || body.bodyType != RigidbodyType2D.Dynamic)
                return;

            var character = body.GetComponent<Character>();
            if (character == null && !TeleportObjects)
                return;
            if (character != null && character.IsDead)
                return;

            if (lastTeleport.TryGetValue(body, out float last) && Time.time < last + Cooldown)
                return;
            lastTeleport[body] = Time.time;

            Vector2 exit = Target.position;
            if (character != null)
            {
                character.Teleport(exit);
            }
            else
            {
                body.position = exit;
                body.transform.position = new Vector3(exit.x, exit.y, body.transform.position.z);
            }

            GameManager.PlaySound(Sound, 0.7f);
        }

        void OnDrawGizmos()
        {
            if (Target == null)
                return;
            Gizmos.color = new Color(0.7f, 0.4f, 1f, 0.8f);
            Gizmos.DrawLine(transform.position, Target.position);
        }
    }
}

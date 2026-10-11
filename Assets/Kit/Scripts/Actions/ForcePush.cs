using System.Collections.Generic;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Launches whatever triggered it in the direction of the green arrow: jump pads, side boosters, cannons...
    /// Rotate the object to change the direction. Use it with a TriggerZone: On Enter > ForcePush > Push.
    /// </summary>
    public class ForcePush : MonoBehaviour
    {
        [Tooltip("How fast things get launched")]
        public float Speed = 13f;
        [Tooltip("Also push balls, crates and projectiles, not only players")]
        public bool PushObjects = true;
        [Tooltip("On = keep the speed sideways to the arrow (feels better for jump pads). Off = fly exactly along the arrow")]
        public bool KeepSidewaysSpeed = true;
        public AudioClip Sound;

        /// <summary>Things touching the pad for a few frames should only be launched once.</summary>
        const float CooldownPerObject = 0.2f;
        readonly Dictionary<Rigidbody2D, float> lastPush = new Dictionary<Rigidbody2D, float>();

        /// <summary>Launches whatever triggered the event.</summary>
        public void Push()
        {
            Rigidbody2D target = TriggeredBy.Body;
            if (target == null || target.bodyType != RigidbodyType2D.Dynamic)
                return;

            var character = target.GetComponent<Character>();
            if (character == null && !PushObjects)
                return;
            if (character != null && character.IsDead)
                return;

            if (this.lastPush.TryGetValue(target, out float last) && Time.time < last + CooldownPerObject)
                return;
            this.lastPush[target] = Time.time;

            Vector2 direction = transform.up;
            Vector2 velocity = target.linearVelocity;
            Vector2 sideways = KeepSidewaysSpeed ? velocity - Vector2.Dot(velocity, direction) * direction : Vector2.zero;
            target.linearVelocity = sideways + direction * Speed;

            GameManager.PlaySound(Sound, 0.8f);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Vector3 from = transform.position;
            Vector3 to = from + transform.up * 1.5f;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawLine(to, to - transform.up * 0.4f + transform.right * 0.3f);
            Gizmos.DrawLine(to, to - transform.up * 0.4f - transform.right * 0.3f);
        }
    }
}

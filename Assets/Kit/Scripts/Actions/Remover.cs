using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Removes things: whatever triggered it (crates, dropped weapons...) or this object itself.
    /// Balls are never removed, they go back to their start instead. Players are not removed (use a Damager).
    /// </summary>
    public class Remover : MonoBehaviour
    {
        [Tooltip("Seconds before a ball goes back to its start")]
        [Min(0f)] public float BallResetDelay = 0f;

        /// <summary>Removes whatever triggered the event.</summary>
        public void RemoveObject()
        {
            GameObject thing = TriggeredBy.Thing;
            if (thing == null || thing.GetComponentInParent<Character>() != null)
                return;

            var ball = thing.GetComponentInParent<Ball>();
            if (ball != null)
            {
                if (!ball.IsResetting)
                    ball.ResetBall(BallResetDelay);
                return;
            }

            Rigidbody2D body = thing.GetComponentInParent<Rigidbody2D>();
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                Destroy(body.gameObject);
        }

        /// <summary>Removes this object, e.g. a wall that disappears when a lever is pulled.</summary>
        public void RemoveSelf()
        {
            Destroy(gameObject);
        }
    }
}

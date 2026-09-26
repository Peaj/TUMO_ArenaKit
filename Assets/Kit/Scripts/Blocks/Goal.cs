using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A goal for balls. When a ball gets in, a player gets a point.
    /// Volleyball: put one on the floor of each half, "Point For Player" = the player on the other side.
    /// Football: "Point For Player" = 0, then the player who touched the ball last scores.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Goal : MonoBehaviour
    {
        [Tooltip("Player who gets the point. 0 = the player who touched the ball last")]
        [Range(0, GameManager.MaxPlayers)] public int PointForPlayer = 0;
        [Tooltip("Put the ball back to its start after a goal")]
        public bool ResetBall = true;
        public AudioClip GoalSound;

        [Header("Events")]
        [Tooltip("Called with the player who scored (0 = nobody)")]
        public UnityEvent<int> OnGoal = new UnityEvent<int>();

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var ball = other.GetComponentInParent<Ball>();
            if (ball == null || ball.IsResetting)
                return;

            int scorer = PointForPlayer > 0 ? PointForPlayer : ball.LastTouchedBy;
            GameManager.PlaySound(GoalSound);
            OnGoal.Invoke(scorer);

            if (ResetBall)
                ball.ResetBall(0.5f);

            if (scorer > 0 && LevelRules.Current != null)
                LevelRules.Current.AddPoint(scorer);
        }

        void OnDrawGizmos()
        {
            Color color = PointForPlayer > 0 ? GameManager.GetPlayerColor(PointForPlayer) : Color.white;
            Gizmos2D.DrawCollider(GetComponent<Collider2D>(), new Color(color.r, color.g, color.b, 0.25f));
            Gizmos2D.DrawLabel(transform.position + Vector3.up * 0.6f, PointForPlayer > 0 ? $"Tor: Punkt für P{PointForPlayer}" : "Tor", color);
        }
    }
}

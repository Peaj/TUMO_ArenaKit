using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// King of the hill: players get points for staying inside the zone.
    /// Set "Points To Win" in the level rules, e.g. 10.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ScoreZone : MonoBehaviour
    {
        [Tooltip("Seconds a player has to stay inside for one point")]
        [Min(0.1f)] public float SecondsPerPoint = 1f;
        [Tooltip("Only counts while a single player is inside")]
        public bool OnlyWhenAlone = true;

        [Header("Look")]
        [Tooltip("Gets the color of the player who holds the zone")]
        public SpriteRenderer Renderer;
        [Range(0f, 1f)] public float Alpha = 0.35f;
        public AudioClip PointSound;

        [Header("Events")]
        [Tooltip("Called with the player who got a point")]
        public UnityEvent<int> OnPoint = new UnityEvent<int>();

        readonly Dictionary<Character, int> inside = new Dictionary<Character, int>();
        readonly Dictionary<int, float> progress = new Dictionary<int, float>();
        readonly List<Character> counting = new List<Character>();

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            SetColor(Color.white);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null)
                return;
            this.inside.TryGetValue(character, out int colliders);
            this.inside[character] = colliders + 1;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null || !this.inside.TryGetValue(character, out int colliders))
                return;
            if (colliders > 1)
                this.inside[character] = colliders - 1;
            else
                this.inside.Remove(character);
        }

        void Update()
        {
            LevelRules rules = LevelRules.Current;

            this.counting.Clear();
            foreach (Character character in this.inside.Keys)
                if (character != null && !character.IsDead)
                    this.counting.Add(character);

            bool contested = OnlyWhenAlone && this.counting.Count > 1;
            if (this.counting.Count == 0)
                SetColor(Color.white);
            else if (contested)
                SetColor(Color.gray);
            else
                SetColor(GameManager.GetPlayerColor(this.counting[0].PlayerNumber));

            if (contested || rules == null || !rules.IsRunning || rules.IsRoundOver)
                return;

            foreach (Character character in this.counting)
            {
                int player = character.PlayerNumber;
                this.progress.TryGetValue(player, out float time);
                time += Time.deltaTime;
                if (time >= SecondsPerPoint)
                {
                    time -= SecondsPerPoint;
                    GameManager.PlaySound(PointSound, 0.5f);
                    OnPoint.Invoke(player);
                    rules.AddPoint(player);
                }
                this.progress[player] = time;
            }
        }

        void SetColor(Color color)
        {
            if (Renderer != null)
                Renderer.color = new Color(color.r, color.g, color.b, Alpha);
        }

        void OnDrawGizmos()
        {
            Gizmos2D.DrawCollider(GetComponent<Collider2D>(), new Color(1f, 0.9f, 0.2f, 0.2f));
        }
    }
}

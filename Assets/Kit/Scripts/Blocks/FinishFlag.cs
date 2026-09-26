using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// The first player to reach the flag wins the round (or gets points). For races and parkour levels.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FinishFlag : MonoBehaviour
    {
        [Tooltip("0 = every player can finish here. 1-4 = only this player")]
        [Range(0, GameManager.MaxPlayers)] public int OnlyForPlayer = 0;
        [Tooltip("Points for reaching the flag. 0 = win the round instantly. With points the player goes back to the start")]
        [Min(0)] public int Points = 0;
        public AudioClip Sound;

        [Header("Look")]
        public SpriteRenderer Renderer;
        [Tooltip("Sprites that are shown one after another (waving flag)")]
        public Sprite[] WaveSprites;

        [Header("Events")]
        [Tooltip("Called with the player who reached the flag")]
        public UnityEvent<int> OnReached = new UnityEvent<int>();

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Update()
        {
            if (Renderer != null && WaveSprites != null && WaveSprites.Length > 0)
                Renderer.sprite = WaveSprites[(int)(Time.time * 4f) % WaveSprites.Length];
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null || character.IsDead)
                return;
            if (OnlyForPlayer != 0 && character.PlayerNumber != OnlyForPlayer)
                return;

            LevelRules rules = LevelRules.Current;
            if (rules == null || rules.IsRoundOver)
                return;

            GameManager.PlaySound(Sound);
            OnReached.Invoke(character.PlayerNumber);

            if (Points <= 0)
            {
                rules.WinRound(character.PlayerNumber);
                return;
            }

            rules.AddPoints(character.PlayerNumber, Points);
            if (!rules.IsRoundOver)
                rules.SendToSpawn(character);
        }
    }
}

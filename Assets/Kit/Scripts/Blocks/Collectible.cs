using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Coins, gems, keys... Players collect them by touching. Gives points and/or calls an event.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [Tooltip("Points for the player who collects it. 0 = no points (e.g. a key that opens a door)")]
        public int Points = 1;
        [Tooltip("Comes back after these seconds. 0 = never")]
        [Min(0f)] public float RespawnTime = 0f;
        public AudioClip Sound;

        [Header("Events")]
        [Tooltip("Called with the player who collected it")]
        public UnityEvent<int> OnCollected = new UnityEvent<int>();

        Collider2D trigger;
        SpriteRenderer[] renderers;
        Vector3 start;
        float respawnAt = -1f;

        void Awake()
        {
            this.trigger = GetComponent<Collider2D>();
            this.trigger.isTrigger = true;
            this.renderers = GetComponentsInChildren<SpriteRenderer>();
            this.start = transform.localPosition;
        }

        void Update()
        {
            transform.localPosition = this.start + Vector3.up * (Mathf.Sin(Time.time * 3f + this.start.x) * 0.08f);

            if (this.respawnAt > 0f && Time.time >= this.respawnAt)
            {
                this.respawnAt = -1f;
                SetVisible(true);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var character = other.GetComponentInParent<Character>();
            if (character == null || character.IsDead)
                return;

            GameManager.PlaySound(Sound, 0.7f);
            OnCollected.Invoke(character.PlayerNumber);
            if (Points != 0 && LevelRules.Current != null)
                LevelRules.Current.AddPoints(character.PlayerNumber, Points);

            if (RespawnTime > 0f)
            {
                SetVisible(false);
                this.respawnAt = Time.time + RespawnTime;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void SetVisible(bool visible)
        {
            this.trigger.enabled = visible;
            foreach (SpriteRenderer spriteRenderer in this.renderers)
                spriteRenderer.enabled = visible;
        }
    }
}

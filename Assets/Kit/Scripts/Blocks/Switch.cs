using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A lever or button. Players flip it by touching or shooting it.
    /// Connect the events to doors, movers, explosives, spawners...
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Switch : MonoBehaviour, IShootable
    {
        [Tooltip("Is the switch on at the start?")]
        public bool IsOn = false;
        [Tooltip("Players flip the switch by walking into it")]
        public bool FlipOnTouch = true;
        [Tooltip("Players flip the switch by shooting it")]
        public bool FlipWhenShot = true;
        [Tooltip("Flips back by itself after these seconds. 0 = stays")]
        [Min(0f)] public float AutoResetTime = 0f;

        [Header("Look")]
        public SpriteRenderer Renderer;
        public Sprite OffSprite;
        public Sprite OnSprite;
        public AudioClip Sound;

        [Header("Events")]
        [Tooltip("Called with the player who flipped it (0 = no player)")]
        public UnityEvent<int> OnTurnedOn = new UnityEvent<int>();
        [Tooltip("Called with the player who flipped it (0 = no player)")]
        public UnityEvent<int> OnTurnedOff = new UnityEvent<int>();

        const float MinTimeBetweenFlips = 0.4f;
        float lastFlip = -999f;
        Coroutine resetRoutine;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            UpdateLook();
        }

        public void Toggle()
        {
            SetState(!IsOn, 0);
        }

        public void TurnOn()
        {
            SetState(true, 0);
        }

        public void TurnOff()
        {
            SetState(false, 0);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!FlipOnTouch)
                return;

            var character = other.GetComponentInParent<Character>();
            if (character != null && !character.IsDead)
                Flip(character.PlayerNumber);
        }

        public void OnShot(Projectile projectile)
        {
            if (FlipWhenShot)
                Flip(projectile.Owner);
        }

        void Flip(int player)
        {
            if (Time.time < this.lastFlip + MinTimeBetweenFlips)
                return;
            SetState(!IsOn, player);
        }

        void SetState(bool on, int player)
        {
            if (on == IsOn)
                return;

            IsOn = on;
            this.lastFlip = Time.time;
            UpdateLook();
            GameManager.PlaySound(Sound);

            if (this.resetRoutine != null)
                StopCoroutine(this.resetRoutine);
            if (on && AutoResetTime > 0f)
                this.resetRoutine = StartCoroutine(ResetLater());

            if (on)
                OnTurnedOn.Invoke(player);
            else
                OnTurnedOff.Invoke(player);
        }

        IEnumerator ResetLater()
        {
            yield return new WaitForSeconds(AutoResetTime);
            this.resetRoutine = null;
            SetState(false, 0);
        }

        void UpdateLook()
        {
            if (Renderer == null)
                return;
            Sprite sprite = IsOn ? OnSprite : OffSprite;
            if (sprite != null)
                Renderer.sprite = sprite;
        }
    }
}

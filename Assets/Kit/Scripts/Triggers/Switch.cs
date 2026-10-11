using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Something that is on or off: a lever, a button, a light...
    /// Flip it from other events (Toggle, Turn On, Turn Off). The Lever prefab flips it with a
    /// TriggerZone (walk into it) and a HitSensor (shoot it).
    /// </summary>
    public class Switch : MonoBehaviour
    {
        [Tooltip("Is the switch on at the start?")]
        public bool IsOn = false;
        [Tooltip("Turns off by itself after these seconds. 0 = stays on")]
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

        /// <summary>Walking into a lever and shooting it at once should not flip it twice.</summary>
        const float MinTimeBetweenToggles = 0.4f;
        float lastToggle = -999f;
        Coroutine resetRoutine;

        void Awake()
        {
            UpdateLook();
        }

        public void Toggle()
        {
            if (Time.time < this.lastToggle + MinTimeBetweenToggles)
                return;
            this.lastToggle = Time.time;
            SetState(!IsOn);
        }

        public void TurnOn()
        {
            SetState(true);
        }

        public void TurnOff()
        {
            SetState(false);
        }

        void SetState(bool on)
        {
            if (on == IsOn)
                return;

            IsOn = on;
            UpdateLook();
            GameManager.PlaySound(Sound);

            if (this.resetRoutine != null)
                StopCoroutine(this.resetRoutine);
            if (on && AutoResetTime > 0f && isActiveAndEnabled)
                this.resetRoutine = StartCoroutine(ResetLater());

            // Runs inside the event that flipped the switch, so the player is passed on.
            if (on)
                OnTurnedOn.Invoke(TriggeredBy.Player);
            else
                OnTurnedOff.Invoke(TriggeredBy.Player);
        }

        IEnumerator ResetLater()
        {
            yield return new WaitForSeconds(AutoResetTime);
            this.resetRoutine = null;
            SetState(false);
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

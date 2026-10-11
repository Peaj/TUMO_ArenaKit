using System.Collections;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Shows another picture: the spring of a jump pad pops out, the light of a mine starts blinking...
    /// </summary>
    public class SwapSprite : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        [Tooltip("The other picture")]
        public Sprite Sprite;
        [Tooltip("Switch between both pictures while it is shown (blinking light)")]
        public bool Blink = false;
        [Tooltip("Blinks per second")]
        [Min(0.5f)] public float BlinkSpeed = 6f;

        Sprite original;
        bool showing;

        void Awake()
        {
            if (Renderer == null)
                Renderer = GetComponent<SpriteRenderer>();
            if (Renderer != null)
                this.original = Renderer.sprite;
        }

        void Update()
        {
            if (this.showing && Blink && Renderer != null)
                Renderer.sprite = Mathf.Repeat(Time.time * BlinkSpeed, 1f) < 0.5f ? Sprite : this.original;
        }

        /// <summary>Shows the other picture until "Restore" is called.</summary>
        public void Show()
        {
            StopAllCoroutines();
            SetShowing(true);
        }

        /// <summary>Shows the other picture for a moment (seconds).</summary>
        public void ShowFor(float seconds)
        {
            StopAllCoroutines();
            SetShowing(true);
            if (isActiveAndEnabled)
                StartCoroutine(RestoreLater(seconds));
        }

        /// <summary>Back to the normal picture.</summary>
        public void Restore()
        {
            StopAllCoroutines();
            SetShowing(false);
        }

        IEnumerator RestoreLater(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            SetShowing(false);
        }

        void SetShowing(bool show)
        {
            this.showing = show;
            if (Renderer != null)
                Renderer.sprite = show && Sprite != null ? Sprite : this.original;
        }
    }
}

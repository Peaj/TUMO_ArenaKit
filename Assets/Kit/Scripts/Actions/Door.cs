using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Blocks the way while it is closed. Open it with a switch, a trigger zone, a counter...
    /// </summary>
    public class Door : MonoBehaviour
    {
        [System.Serializable]
        public struct DoorPart
        {
            public SpriteRenderer Renderer;
            public Sprite ClosedSprite;
            [Tooltip("Empty = the part becomes see-through when open")]
            public Sprite OpenSprite;
        }

        [Tooltip("Is the door open at the start?")]
        public bool IsOpen = false;
        [Tooltip("Closes again by itself after these seconds. 0 = stays open")]
        [Min(0f)] public float AutoCloseTime = 0f;
        public AudioClip OpenSound;
        public AudioClip CloseSound;

        [Header("Look")]
        [Tooltip("Parts of the door that change when it opens")]
        public DoorPart[] Parts;
        [Tooltip("Colliders that block the way while the door is closed. Empty = all solid colliders of this object")]
        public Collider2D[] BlockingColliders;

        [Header("Events")]
        public UnityEvent OnOpened = new UnityEvent();
        public UnityEvent OnClosed = new UnityEvent();

        Coroutine closeRoutine;

        void Awake()
        {
            if (BlockingColliders == null || BlockingColliders.Length == 0)
                BlockingColliders = GetComponents<Collider2D>();
            Apply();
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void Toggle()
        {
            SetOpen(!IsOpen);
        }

        void SetOpen(bool open)
        {
            if (this.closeRoutine != null)
            {
                StopCoroutine(this.closeRoutine);
                this.closeRoutine = null;
            }

            if (open && AutoCloseTime > 0f)
                this.closeRoutine = StartCoroutine(CloseLater());

            if (open == IsOpen)
                return;

            IsOpen = open;
            Apply();
            GameManager.PlaySound(open ? OpenSound : CloseSound);
            if (open)
                OnOpened.Invoke();
            else
                OnClosed.Invoke();
        }

        IEnumerator CloseLater()
        {
            yield return new WaitForSeconds(AutoCloseTime);
            this.closeRoutine = null;
            SetOpen(false);
        }

        void Apply()
        {
            if (BlockingColliders != null)
                foreach (Collider2D blocker in BlockingColliders)
                    if (blocker != null && !blocker.isTrigger)
                        blocker.enabled = !IsOpen;

            if (Parts == null)
                return;

            foreach (DoorPart part in Parts)
            {
                if (part.Renderer == null)
                    continue;

                if (IsOpen && part.OpenSprite == null)
                {
                    part.Renderer.sprite = part.ClosedSprite != null ? part.ClosedSprite : part.Renderer.sprite;
                    part.Renderer.color = new Color(1f, 1f, 1f, 0.25f);
                }
                else
                {
                    Sprite sprite = IsOpen ? part.OpenSprite : part.ClosedSprite;
                    if (sprite != null)
                        part.Renderer.sprite = sprite;
                    part.Renderer.color = Color.white;
                }
            }
        }
    }
}

using UnityEngine;

namespace ArenaKit
{
    /// <summary>Plays pictures one after another in a loop, e.g. a waving flag.</summary>
    public class SpriteAnimation : MonoBehaviour
    {
        public SpriteRenderer Renderer;
        public Sprite[] Sprites;
        [Min(0.1f)] public float FramesPerSecond = 4f;

        void Awake()
        {
            if (Renderer == null)
                Renderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (Renderer != null && Sprites != null && Sprites.Length > 0)
                Renderer.sprite = Sprites[(int)(Time.time * FramesPerSecond) % Sprites.Length];
        }
    }
}

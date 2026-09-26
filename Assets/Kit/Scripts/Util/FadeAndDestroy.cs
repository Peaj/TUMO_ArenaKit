using UnityEngine;

namespace ArenaKit
{
    /// <summary>Fades out all sprites of the object and destroys it.</summary>
    public class FadeAndDestroy : MonoBehaviour
    {
        public float Duration = 1f;

        SpriteRenderer[] renderers;
        float start;

        void Start()
        {
            this.renderers = GetComponentsInChildren<SpriteRenderer>();
            this.start = Time.time;
            Destroy(gameObject, Duration);
        }

        void Update()
        {
            float alpha = 1f - Mathf.Clamp01((Time.time - this.start) / Duration);
            foreach (SpriteRenderer spriteRenderer in this.renderers)
            {
                if (spriteRenderer == null)
                    continue;
                Color color = spriteRenderer.color;
                spriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
            }
        }
    }
}

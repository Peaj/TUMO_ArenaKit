using System.Collections;
using UnityEngine;

namespace ArenaKit
{
    /// <summary>
    /// Makes this object disappear (and come back later, if you like): collected coins, keys, a wall that vanishes...
    /// </summary>
    public class Disappear : MonoBehaviour
    {
        [Tooltip("Comes back after these seconds. 0 = gone for good")]
        [Min(0f)] public float RespawnTime = 0f;

        public bool IsHidden { get; private set; }

        Collider2D[] colliders;
        Renderer[] renderers;

        void Awake()
        {
            this.colliders = GetComponentsInChildren<Collider2D>();
            this.renderers = GetComponentsInChildren<Renderer>();
        }

        public void Hide()
        {
            if (IsHidden)
                return;

            if (RespawnTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            SetVisible(false);
            if (isActiveAndEnabled)
                StartCoroutine(ShowLater());
        }

        public void Show()
        {
            StopAllCoroutines();
            SetVisible(true);
        }

        IEnumerator ShowLater()
        {
            yield return new WaitForSeconds(RespawnTime);
            SetVisible(true);
        }

        void SetVisible(bool visible)
        {
            IsHidden = !visible;
            foreach (Collider2D own in this.colliders)
                own.enabled = visible;
            foreach (Renderer own in this.renderers)
                own.enabled = visible;
        }
    }
}

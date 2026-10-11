using UnityEngine;

namespace ArenaKit
{
    /// <summary>Floats gently up and down, e.g. coins and keys.</summary>
    public class Hover : MonoBehaviour
    {
        [Tooltip("How far it moves up and down")]
        public float Height = 0.08f;
        [Tooltip("How fast it moves")]
        public float Speed = 3f;

        Vector3 start;

        void Awake()
        {
            this.start = transform.localPosition;
        }

        void Update()
        {
            transform.localPosition = this.start + Vector3.up * (Mathf.Sin(Time.time * Speed + this.start.x) * Height);
        }
    }
}

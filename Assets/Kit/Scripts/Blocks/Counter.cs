using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Counts up (e.g. pressed switches, collected keys) and calls an event when the target is reached.
    /// </summary>
    public class Counter : MonoBehaviour
    {
        [Tooltip("Count needed for the event")]
        [Min(1)] public int Target = 3;
        [Tooltip("Start from 0 again after reaching the target")]
        public bool ResetWhenReached = false;

        [Header("Events")]
        public UnityEvent OnReached = new UnityEvent();
        [Tooltip("Called with the new count")]
        public UnityEvent<int> OnCountChanged = new UnityEvent<int>();

        public int Count { get; private set; }

        public void Add()
        {
            SetCount(Count + 1);
        }

        public void Remove()
        {
            SetCount(Mathf.Max(0, Count - 1));
        }

        public void ResetCount()
        {
            SetCount(0);
        }

        void SetCount(int value)
        {
            if (value == Count)
                return;

            bool wasBelow = Count < Target;
            Count = value;
            OnCountChanged.Invoke(Count);

            if (wasBelow && Count >= Target)
            {
                OnReached.Invoke();
                if (ResetWhenReached)
                    Count = 0;
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
            Gizmos2D.DrawLabel(transform.position + Vector3.up * 0.7f, $"Counter {Count}/{Target}", Gizmos.color);
        }
    }
}

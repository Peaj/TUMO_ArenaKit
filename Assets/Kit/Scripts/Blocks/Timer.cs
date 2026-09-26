using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Calls an event after some seconds, e.g. "after 30 seconds the lava starts rising".
    /// </summary>
    public class Timer : MonoBehaviour
    {
        [Tooltip("Seconds until the event")]
        [Min(0f)] public float Seconds = 10f;
        [Tooltip("Starts counting when the round starts")]
        public bool StartAutomatically = true;
        [Tooltip("Starts again when finished")]
        public bool Repeat = false;

        [Header("Events")]
        public UnityEvent OnFinished = new UnityEvent();

        public float TimeLeft { get; private set; }
        public bool IsRunning { get; private set; }

        void Start()
        {
            TimeLeft = Seconds;
            IsRunning = StartAutomatically;
        }

        void Update()
        {
            if (!IsRunning)
                return;

            // Wait for the start of the round (players can't move before).
            if (LevelRules.Current != null && !LevelRules.Current.IsRunning)
                return;

            TimeLeft -= Time.deltaTime;
            if (TimeLeft > 0f)
                return;

            OnFinished.Invoke();
            if (Repeat)
            {
                TimeLeft += Seconds;
            }
            else
            {
                TimeLeft = 0f;
                IsRunning = false;
            }
        }

        public void StartTimer()
        {
            TimeLeft = Seconds;
            IsRunning = true;
        }

        public void StopTimer()
        {
            IsRunning = false;
        }

        public void ContinueTimer()
        {
            IsRunning = true;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.3f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 0.2f);
            Gizmos2D.DrawLabel(transform.position + Vector3.up * 0.7f, $"Timer {Seconds:0.#}s", Gizmos.color);
        }
    }
}

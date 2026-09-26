using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Creates objects again and again: weapon pickups, balls, falling bombs...
    /// </summary>
    public class Spawner : MonoBehaviour
    {
        [Tooltip("What to spawn. With more than one, a random one is picked")]
        public GameObject[] Prefabs;
        [Tooltip("Seconds between spawns. 0 = only when an event calls Spawn")]
        [Min(0f)] public float Interval = 5f;
        [Tooltip("Spawn one right at the start")]
        public bool SpawnAtStart = true;
        [Tooltip("Max objects from this spawner at the same time. 0 = no limit")]
        [Min(0)] public int MaxAlive = 1;
        [Tooltip("Random offset around the spawner (x = left/right, y = up/down)")]
        public Vector2 RandomOffset = Vector2.zero;

        [Header("Events")]
        public UnityEvent OnSpawn = new UnityEvent();

        readonly List<GameObject> alive = new List<GameObject>();

        void Start()
        {
            if (SpawnAtStart)
                Spawn();
            if (Interval > 0f)
                StartCoroutine(SpawnLoop());
        }

        IEnumerator SpawnLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Interval);
                Spawn();
            }
        }

        public void Spawn()
        {
            if (Prefabs == null || Prefabs.Length == 0)
                return;

            this.alive.RemoveAll(o => o == null);
            if (MaxAlive > 0 && this.alive.Count >= MaxAlive)
                return;

            GameObject prefab = Prefabs[Random.Range(0, Prefabs.Length)];
            if (prefab == null)
                return;

            Vector3 offset = new Vector3(Random.Range(-RandomOffset.x, RandomOffset.x), Random.Range(-RandomOffset.y, RandomOffset.y), 0f);
            this.alive.Add(Instantiate(prefab, transform.position + offset, prefab.transform.rotation));
            OnSpawn.Invoke();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position + Vector3.left * 0.25f, transform.position + Vector3.right * 0.25f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.25f, transform.position + Vector3.up * 0.25f);
            if (RandomOffset != Vector2.zero)
                Gizmos.DrawWireCube(transform.position, RandomOffset * 2f);
            Gizmos2D.DrawLabel(transform.position + Vector3.up * 0.7f, "Spawner", Gizmos.color);
        }
    }
}

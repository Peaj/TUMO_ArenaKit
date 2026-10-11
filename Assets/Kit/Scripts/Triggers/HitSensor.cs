using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// Calls an event when it gets shot or caught in an explosion. Shoot a lever to flip it, shoot a mine to blow it up...
    /// It needs a collider (a trigger collider is fine: projectiles still hit it).
    /// </summary>
    public class HitSensor : MonoBehaviour, IShootable, IDamageable
    {
        [Tooltip("React to projectiles that hit it")]
        public bool Shots = true;
        [Tooltip("React to explosions nearby")]
        public bool Explosions = true;

        [Header("Events")]
        [Tooltip("Called with the player who shot (0 = no player)")]
        public UnityEvent<int> OnHit = new UnityEvent<int>();

        int lastHitFrame = -1;

        public void OnShot(Projectile projectile)
        {
            if (Shots)
                Hit(projectile.Owner);
        }

        public void TakeDamage(float damage, int attacker)
        {
            if (Explosions)
                Hit(attacker);
        }

        void Hit(int player)
        {
            // A rocket hits directly and explodes in the same moment: count that once.
            if (Time.frameCount == this.lastHitFrame || !isActiveAndEnabled)
                return;
            this.lastHitFrame = Time.frameCount;
            TriggeredBy.Invoke(OnHit, TriggeredBy.PlayerObject(player), player);
        }
    }
}

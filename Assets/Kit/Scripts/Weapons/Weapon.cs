using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A weapon a player can hold. Make your own weapon as a prefab variant and change the values.
    /// It shoots in the direction the Muzzle points (the yellow arrow in the Scene view):
    /// rotate the weapon (or the Muzzle) to shoot upwards, like the grenade launcher.
    /// When the ammo is used up the weapon drops to the ground.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Projectile")]
        [Tooltip("What the weapon shoots")]
        public Projectile Projectile;
        [Tooltip("How fast the projectiles fly")]
        public float ProjectileSpeed = 15f;
        [Tooltip("Projectiles per shot (shotgun: 6)")]
        [Min(1)] public int ProjectilesPerShot = 1;
        [Tooltip("Random spread in degrees")]
        [Range(0f, 90f)] public float Spread = 0f;

        [Header("Firing")]
        [Tooltip("Seconds between two shots")]
        [Min(0.02f)] public float FireDelay = 0.5f;
        [Tooltip("Keep shooting while the button is held down")]
        public bool Automatic = false;
        [Tooltip("How hard the weapon pushes the player back when shooting")]
        public float Recoil = 3f;
        [Tooltip("Shots until the weapon is empty and drops. -1 = never empty")]
        public int Ammo = 10;

        [Header("Parts")]
        [Tooltip("Where the projectiles come out. They fly in the direction of its red (x) arrow")]
        public Transform Muzzle;
        public AudioClip FireSound;
        [Range(0f, 1f)] public float FireVolume = 0.7f;

        [Header("Events")]
        public UnityEvent OnFire = new UnityEvent();
        public UnityEvent OnEmpty = new UnityEvent();

        public Character Owner { get; private set; }

        const float DelayAfterPickup = 0.15f;
        float lastFired = -999f;

        /// <summary>Called by the player who picked up this weapon.</summary>
        public void OnEquipped(Character owner)
        {
            Owner = owner;
            this.lastFired = Time.time - FireDelay + DelayAfterPickup;
        }

        public bool TryFire()
        {
            if (Owner == null || Projectile == null || Ammo == 0)
                return false;

            if (Time.time < this.lastFired + FireDelay)
                return false;

            this.lastFired = Time.time;

            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
            Vector2 aim = AimDirection();

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                Vector2 direction = Quaternion.Euler(0f, 0f, Random.Range(-0.5f, 0.5f) * Spread) * aim;
                // Only turn around the z axis (FromToRotation would flip left shots around a random axis).
                var rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                Projectile projectile = Instantiate(Projectile, origin, rotation);
                projectile.Launch(Owner, direction * ProjectileSpeed);
            }

            if (Recoil != 0f)
                Owner.Body.AddForce(-aim * Recoil, ForceMode2D.Impulse);

            GameManager.PlaySound(FireSound, FireVolume);
            OnFire.Invoke();

            if (Ammo > 0)
            {
                Ammo--;
                if (Ammo == 0)
                {
                    OnEmpty.Invoke();
                    Owner.DropWeapon();
                }
            }

            return true;
        }

        /// <summary>Where the muzzle points in the world. Also correct when the player looks to the left (mirrored).</summary>
        public Vector2 AimDirection()
        {
            Transform from = Muzzle != null ? Muzzle : transform;
            Vector2 direction = from.TransformVector(Vector3.right);
            return direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.right;
        }

        void OnDrawGizmosSelected()
        {
            Transform from = Muzzle != null ? Muzzle : transform;
            Vector3 start = from.position;
            Vector3 end = start + (Vector3)AimDirection() * 1.2f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, end);
            Vector3 side = Quaternion.Euler(0f, 0f, 90f) * (end - start).normalized * 0.15f;
            Gizmos.DrawLine(end, end - (end - start).normalized * 0.3f + side);
            Gizmos.DrawLine(end, end - (end - start).normalized * 0.3f - side);
        }
    }
}

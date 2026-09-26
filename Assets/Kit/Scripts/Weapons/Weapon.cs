using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A weapon a player can hold. Make your own weapon as a prefab variant and change the values.
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
        [Tooltip("Aim upwards in degrees (grenade launcher: 20)")]
        [Range(-45f, 80f)] public float AimAngle = 0f;

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
        [Tooltip("Where the projectiles come out")]
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

            float facing = Owner.Facing;
            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
            Vector2 aim = Direction(AimAngle, facing);

            for (int i = 0; i < ProjectilesPerShot; i++)
            {
                float angle = AimAngle + Random.Range(-0.5f, 0.5f) * Spread;
                Vector2 direction = Direction(angle, facing);
                Projectile projectile = Instantiate(Projectile, origin, Quaternion.FromToRotation(Vector3.right, direction));
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

        static Vector2 Direction(float angle, float facing)
        {
            float radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians) * facing, Mathf.Sin(radians));
        }
    }
}

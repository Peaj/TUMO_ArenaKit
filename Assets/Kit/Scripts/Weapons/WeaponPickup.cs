using UnityEngine;
using UnityEngine.Events;

namespace ArenaKit
{
    /// <summary>
    /// A weapon lying in the level. Players pick it up by walking into it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class WeaponPickup : MonoBehaviour
    {
        [Tooltip("The weapon prefab players get")]
        public Weapon Weapon;
        [Tooltip("Optional: every time the pickup (re)appears it becomes a random weapon from this list")]
        public Weapon[] RandomWeapons;
        [Tooltip("Seconds until a new weapon appears. 0 = never comes back")]
        [Min(0f)] public float RespawnTime = 8f;
        [Tooltip("Players who already hold a weapon swap it for this one")]
        public bool SwapWeapons = true;
        public AudioClip PickupSound;

        [Header("Look")]
        [Tooltip("Shows the weapon (filled in automatically)")]
        public SpriteRenderer Icon;

        [Header("Events")]
        [Tooltip("Called with the number of the player who picked it up")]
        public UnityEvent<int> OnPickedUp = new UnityEvent<int>();

        /// <summary>Scale of a weapon in the hand of the standard player.</summary>
        const float HandScale = 0.48f;

        bool available = true;
        float respawnAt;
        Vector3 iconCenter;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            PickRandomWeapon();
            UpdateIcon();
        }

        void Update()
        {
            if (!this.available && RespawnTime > 0f && Time.time >= this.respawnAt)
                SetAvailable(true);

            if (Icon != null)
                Icon.transform.localPosition = this.iconCenter + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryPickUp(other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            TryPickUp(other);
        }

        void TryPickUp(Collider2D other)
        {
            if (!this.available || Weapon == null)
                return;

            var character = other.GetComponentInParent<Character>();
            if (character == null || character.IsDead)
                return;
            if (!SwapWeapons && character.CurrentWeapon != null)
                return;

            character.Equip(Weapon);
            GameManager.PlaySound(PickupSound);
            OnPickedUp.Invoke(character.PlayerNumber);

            if (RespawnTime > 0f)
            {
                SetAvailable(false);
                this.respawnAt = Time.time + RespawnTime;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void SetAvailable(bool value)
        {
            this.available = value;
            if (value)
            {
                PickRandomWeapon();
                UpdateIcon();
            }
            if (Icon != null)
                Icon.enabled = value;
        }

        void PickRandomWeapon()
        {
            if (RandomWeapons == null || RandomWeapons.Length == 0)
                return;
            Weapon random = RandomWeapons[Random.Range(0, RandomWeapons.Length)];
            if (random != null)
                Weapon = random;
        }

        void OnValidate()
        {
#if UNITY_EDITOR
            // Changing sprites directly in OnValidate is not allowed, so do it a moment later.
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                    UpdateIcon();
            };
#endif
        }

        /// <summary>Shows the sprite of the weapon on the pickup.</summary>
        public void UpdateIcon()
        {
            if (Icon == null)
                return;

            SpriteRenderer weaponRenderer = Weapon != null ? Weapon.GetComponentInChildren<SpriteRenderer>() : null;
            Icon.sprite = weaponRenderer != null ? weaponRenderer.sprite : null;
            Icon.color = weaponRenderer != null ? weaponRenderer.color : Color.white;
            Icon.transform.localScale = Weapon != null ? Weapon.transform.localScale * HandScale : Vector3.one * HandScale;

            // Weapon sprites have their pivot at the grip: move the icon so the weapon is centered on the pickup.
            this.iconCenter = Icon.sprite != null ? -Vector3.Scale(Icon.sprite.bounds.center, Icon.transform.localScale) : Vector3.zero;
            if (!Application.isPlaying)
                Icon.transform.localPosition = this.iconCenter;
        }
    }
}

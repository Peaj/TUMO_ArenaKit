using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace ArenaKit
{
    /// <summary>
    /// A player: runs, jumps, holds a weapon and can be hurt.
    /// The movement is physics based, so explosions and recoil push the player around.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Character : MonoBehaviour, IDamageable
    {
        [Header("Health")]
        [Tooltip("How much damage the player can take. Most weapons do 100 damage")]
        public float Health = 100f;

        [Header("Movement")]
        [Tooltip("How strongly the player pushes to the side")]
        public float MoveForce = 10000f;
        [Tooltip("Maximum running speed")]
        public float MaxSpeed = 10f;
        [Tooltip("How strongly the player jumps")]
        public float JumpForce = 500f;
        [Tooltip("Extra jumps in the air (1 = double jump)")]
        [Min(0)] public int ExtraJumps = 0;
        [Tooltip("How well the player can steer in the air (0 = not at all, 1 = like on the ground)")]
        [Range(0f, 1f)] public float AirControl = 1f;

        [Header("Weapon")]
        [Tooltip("The weapon gets attached to this object (it moves with the arm animation)")]
        public Transform WeaponMount;

        [Header("Look")]
        [Tooltip("These sprites get the player color (red, blue, green, yellow)")]
        public SpriteRenderer[] ColoredParts;
        [Tooltip("How strong the player color is")]
        [Range(0f, 1f)] public float ColorStrength = 0.6f;
        [Tooltip("Arrow above the head in the full player color")]
        public SpriteRenderer Marker;

        [Header("Sounds")]
        public AudioClip[] JumpSounds;
        public AudioClip[] HurtSounds;
        public AudioClip DeathSound;

        [Header("Events")]
        public UnityEvent OnJumped = new UnityEvent();
        public UnityEvent OnHurt = new UnityEvent();
        public UnityEvent OnDied = new UnityEvent();

        public int PlayerNumber { get; private set; }
        public bool IsDead { get; private set; }
        public bool InputEnabled { get; set; } = true;
        public float CurrentHealth { get; private set; }
        public Weapon CurrentWeapon { get; private set; }
        public Rigidbody2D Body => this.body;
        public float Facing => Mathf.Sign(transform.localScale.x);
        public bool IsGrounded => Time.time - this.lastGroundedTime < 0.05f;

        const float JumpBufferTime = 0.12f;
        const float CoyoteTime = 0.1f;
        const float SpawnProtectionTime = 1f;
        const float KillCreditTime = 4f;
        const float KillHeight = -40f;
        const float DropThroughTime = 0.35f;
        const float StickDeadZone = 0.2f;

        Rigidbody2D body;
        Animator animator;
        PlayerInput playerInput;
        InputAction moveAction;
        InputAction jumpAction;
        InputAction fireAction;
        Collider2D[] colliders;
        SpriteRenderer[] renderers;
        readonly Dictionary<SpriteRenderer, Color> baseColors = new Dictionary<SpriteRenderer, Color>();
        readonly ContactPoint2D[] contacts = new ContactPoint2D[16];
        LevelRules rules;

        Vector3 baseScale;
        Vector2 moveInput;
        float jumpPressedTime = -1f;
        float lastGroundedTime = -1f;
        int extraJumpsLeft;
        float spawnTime;
        int lastAttacker;
        float lastAttackTime = -999f;
        bool flashing;

        // Filled by the collision callbacks of the last physics step.
        bool touchingGround;
        bool touchingWallLeft;
        bool touchingWallRight;
        Rigidbody2D groundBody;
        Collider2D groundCollider;

        void Awake()
        {
            this.body = GetComponent<Rigidbody2D>();
            this.animator = GetComponent<Animator>();
            this.playerInput = GetComponent<PlayerInput>();
            this.colliders = GetComponentsInChildren<Collider2D>();
            this.renderers = GetComponentsInChildren<SpriteRenderer>(true);
            this.baseScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            this.body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            CurrentHealth = Health;
            this.spawnTime = Time.time;
            RememberColors();
        }

        void Start()
        {
            // PlayerInput has set up its own copy of the actions by now.
            if (this.playerInput != null && this.playerInput.actions != null)
            {
                this.moveAction = this.playerInput.actions.FindAction("Move");
                this.jumpAction = this.playerInput.actions.FindAction("Jump");
                this.fireAction = this.playerInput.actions.FindAction("Fire");
            }
        }

        /// <summary>Called by LevelRules right after the player was spawned.</summary>
        public void Setup(int playerNumber, float healthOverride, LevelRules levelRules)
        {
            PlayerNumber = playerNumber;
            this.rules = levelRules;
            if (healthOverride > 0f)
                Health = healthOverride;
            CurrentHealth = Health;
            this.spawnTime = Time.time;
            ApplyPlayerColor(GameManager.GetPlayerColor(playerNumber));
        }

        void Update()
        {
            if (IsDead)
                return;

            if (transform.position.y < KillHeight)
            {
                Die(0);
                return;
            }

            UpdateSpawnProtectionBlink();

            if (!InputEnabled || this.moveAction == null)
            {
                this.moveInput = Vector2.zero;
                SetAnimatorSpeed(0f);
                return;
            }

            this.moveInput = this.moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(this.moveInput.x) < StickDeadZone)
                this.moveInput.x = 0f;

            if (this.jumpAction != null && this.jumpAction.WasPressedThisFrame())
                this.jumpPressedTime = Time.time;

            if (CurrentWeapon != null && this.fireAction != null)
            {
                bool wantsToFire = CurrentWeapon.Automatic ? this.fireAction.IsPressed() : this.fireAction.WasPressedThisFrame();
                if (wantsToFire && CurrentWeapon.TryFire())
                    SetAnimatorTrigger("Shoot");
            }

            FlipCharacterInMoveDirection();
            SetAnimatorSpeed(Mathf.Abs(this.moveInput.x));
        }

        void FixedUpdate()
        {
            if (IsDead)
                return;

            bool grounded = this.touchingGround;
            if (grounded)
            {
                this.lastGroundedTime = Time.time;
                this.extraJumpsLeft = ExtraJumps;

                // Ride along on moving platforms.
                if (this.groundBody != null && this.groundBody.bodyType == RigidbodyType2D.Kinematic)
                    this.body.position += this.groundBody.linearVelocity * Time.fixedDeltaTime;
            }

            Run(grounded);
            Jump(grounded);

            // The collision callbacks of the next physics step fill these again.
            this.touchingGround = false;
            this.touchingWallLeft = false;
            this.touchingWallRight = false;
            this.groundBody = null;
            this.groundCollider = null;
        }

        void Run(bool grounded)
        {
            float horizontal = this.moveInput.x;

            // Pushing into a wall would make the player stick to it.
            if ((horizontal < 0f && this.touchingWallLeft) || (horizontal > 0f && this.touchingWallRight))
                horizontal = 0f;

            float velocity = Mathf.Abs(this.body.linearVelocity.x);
            float factor = 1f - Mathf.Clamp01(velocity / MaxSpeed);

            // Changing direction always gets the full force.
            if (this.body.linearVelocity.x * horizontal < 0f)
                factor = 1f;

            if (!grounded)
                factor *= AirControl;

            this.body.AddForce(new Vector2(MoveForce * horizontal * Time.fixedDeltaTime * factor, 0f));
        }

        void Jump(bool grounded)
        {
            if (Time.time - this.jumpPressedTime > JumpBufferTime)
                return;

            // Down + jump: fall through a one way platform.
            if (this.moveInput.y < -0.5f && grounded && IsOneWayPlatform(this.groundCollider))
            {
                this.jumpPressedTime = -1f;
                StartCoroutine(DropThrough(this.groundCollider));
                return;
            }

            bool canJump = grounded || Time.time - this.lastGroundedTime <= CoyoteTime;
            if (!canJump && this.extraJumpsLeft > 0)
            {
                this.extraJumpsLeft--;
                canJump = true;
            }

            if (!canJump)
                return;

            this.jumpPressedTime = -1f;
            this.lastGroundedTime = -1f;

            Vector2 velocity = this.body.linearVelocity;
            if (velocity.y < 0f)
                this.body.linearVelocity = new Vector2(velocity.x, 0f);

            this.body.AddForce(new Vector2(0f, JumpForce));
            SetAnimatorTrigger("Jump");
            PlayRandom(JumpSounds, 0.5f);
            OnJumped.Invoke();
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            ReadContacts(collision);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            ReadContacts(collision);
        }

        void ReadContacts(Collision2D collision)
        {
            int count = collision.GetContacts(this.contacts);
            for (int i = 0; i < count; i++)
            {
                Vector2 normal = this.contacts[i].normal;
                if (normal.y > 0.6f)
                {
                    this.touchingGround = true;
                    this.groundBody = collision.rigidbody;
                    this.groundCollider = collision.collider;
                }
                else if (normal.x > 0.7f)
                {
                    this.touchingWallLeft = true;
                }
                else if (normal.x < -0.7f)
                {
                    this.touchingWallRight = true;
                }
            }
        }

        static bool IsOneWayPlatform(Collider2D platform)
        {
            if (platform == null || !platform.usedByEffector)
                return false;
            var effector = platform.GetComponent<PlatformEffector2D>();
            return effector != null && effector.useOneWay;
        }

        IEnumerator DropThrough(Collider2D platform)
        {
            foreach (Collider2D own in this.colliders)
                Physics2D.IgnoreCollision(own, platform, true);

            yield return new WaitForSeconds(DropThroughTime);

            if (platform != null)
                foreach (Collider2D own in this.colliders)
                    if (own != null)
                        Physics2D.IgnoreCollision(own, platform, false);
        }

        void FlipCharacterInMoveDirection()
        {
            if (this.moveInput.x < 0f)
                SetFacing(-1f);
            else if (this.moveInput.x > 0f)
                SetFacing(1f);
        }

        void SetFacing(float direction)
        {
            transform.localScale = new Vector3(direction * this.baseScale.x, this.baseScale.y, this.baseScale.z);
        }

        // ---------------------------------------------------------------
        // Weapons
        // ---------------------------------------------------------------

        /// <summary>Gives the player a new weapon (the old one disappears).</summary>
        public void Equip(Weapon weaponPrefab)
        {
            if (weaponPrefab == null || IsDead)
                return;

            RemoveWeapon(drop: false);

            Transform mount = WeaponMount != null ? WeaponMount : transform;
            CurrentWeapon = Instantiate(weaponPrefab, mount, false);
            CurrentWeapon.name = weaponPrefab.name;
            CurrentWeapon.OnEquipped(this);
        }

        /// <summary>Lets the weapon fall to the ground.</summary>
        public void DropWeapon()
        {
            RemoveWeapon(drop: true);
        }

        void RemoveWeapon(bool drop)
        {
            if (CurrentWeapon == null)
                return;

            Weapon weapon = CurrentWeapon;
            CurrentWeapon = null;

            if (drop)
                Debris.Drop(weapon.gameObject, this.body.linearVelocity);
            else
                Destroy(weapon.gameObject);
        }

        // ---------------------------------------------------------------
        // Health
        // ---------------------------------------------------------------

        public bool IsProtected => Time.time - this.spawnTime < SpawnProtectionTime;

        public void TakeDamage(float damage, int attacker)
        {
            if (IsDead || damage <= 0f || IsProtected)
                return;

            if (attacker > 0 && attacker != PlayerNumber)
            {
                this.lastAttacker = attacker;
                this.lastAttackTime = Time.time;
            }

            CurrentHealth -= damage;
            if (CurrentHealth <= 0f)
            {
                CurrentHealth = 0f;
                Die(attacker);
                return;
            }

            PlayRandom(HurtSounds, 0.8f);
            if (!this.flashing)
                StartCoroutine(Flash());
            OnHurt.Invoke();
        }

        /// <summary>Kills the player right away (for events).</summary>
        public void Kill()
        {
            Die(0);
        }

        public void Die(int attacker)
        {
            if (IsDead)
                return;

            IsDead = true;

            // Pushed into the lava by somebody? That still counts as their kill.
            int killer = attacker > 0 && attacker != PlayerNumber
                ? attacker
                : (Time.time - this.lastAttackTime < KillCreditTime ? this.lastAttacker : 0);

            DropWeapon();
            SetAnimatorTrigger("Die");
            foreach (Collider2D own in this.colliders)
                own.enabled = false;

            this.body.linearVelocity = new Vector2(this.body.linearVelocity.x * 0.3f, 8f);
            if (this.playerInput != null)
                this.playerInput.enabled = false;

            SetAlpha(1f);
            GameManager.PlaySound(DeathSound);
            OnDied.Invoke();

            if (this.rules != null)
                this.rules.NotifyPlayerDied(this, killer);

            Destroy(gameObject, 3f);
        }

        /// <summary>Moves the player somewhere else and keeps the speed.</summary>
        public void Teleport(Vector2 position)
        {
            this.body.position = position;
            transform.position = new Vector3(position.x, position.y, transform.position.z);
        }

        // ---------------------------------------------------------------
        // Look and sound
        // ---------------------------------------------------------------

        void RememberColors()
        {
            this.baseColors.Clear();
            foreach (SpriteRenderer spriteRenderer in this.renderers)
                if (spriteRenderer != null)
                    this.baseColors[spriteRenderer] = spriteRenderer.color;
        }

        void ApplyPlayerColor(Color color)
        {
            if (ColoredParts != null)
                foreach (SpriteRenderer part in ColoredParts)
                    if (part != null)
                        part.color = Color.Lerp(Color.white, color, ColorStrength);
            if (Marker != null)
                Marker.color = color;
            RememberColors();
        }

        IEnumerator Flash()
        {
            this.flashing = true;
            for (int i = 0; i < 3; i++)
            {
                foreach (SpriteRenderer spriteRenderer in this.renderers)
                    if (spriteRenderer != null)
                        spriteRenderer.color = new Color(1f, 0.3f, 0.3f, spriteRenderer.color.a);
                yield return new WaitForSeconds(0.06f);
                RestoreColors();
                yield return new WaitForSeconds(0.06f);
            }
            this.flashing = false;
        }

        void RestoreColors()
        {
            foreach (KeyValuePair<SpriteRenderer, Color> pair in this.baseColors)
                if (pair.Key != null)
                    pair.Key.color = pair.Value;
        }

        void UpdateSpawnProtectionBlink()
        {
            if (this.flashing)
                return;
            SetAlpha(IsProtected && InputEnabled ? 0.55f + 0.45f * Mathf.Sin(Time.time * 30f) : 1f);
        }

        void SetAlpha(float alpha)
        {
            foreach (KeyValuePair<SpriteRenderer, Color> pair in this.baseColors)
                if (pair.Key != null)
                    pair.Key.color = new Color(pair.Value.r, pair.Value.g, pair.Value.b, pair.Value.a * alpha);
        }

        void SetAnimatorSpeed(float speed)
        {
            if (this.animator != null && this.animator.runtimeAnimatorController != null)
                this.animator.SetFloat("Speed", speed);
        }

        void SetAnimatorTrigger(string trigger)
        {
            if (this.animator != null && this.animator.runtimeAnimatorController != null)
                this.animator.SetTrigger(trigger);
        }

        static void PlayRandom(AudioClip[] clips, float volume = 1f)
        {
            if (clips == null || clips.Length == 0)
                return;
            GameManager.PlaySound(clips[Random.Range(0, clips.Length)], volume);
        }
    }
}

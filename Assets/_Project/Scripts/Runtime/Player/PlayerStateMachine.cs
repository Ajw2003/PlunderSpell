using Interfaces;
using StateMachine.States;
using UnityEngine;

namespace StateMachine
{
    // The merged controller: Rigidbody-based movement driven against standard world gravity
    // (Physics.gravity, -Y). The Rigidbody uses Unity's built-in gravity (useGravity = true) and
    // rotation is frozen so the body stays upright; PlayerWalkState/PlayerJumpState express
    // movement against world Vector3.up.
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerStateMachine : BaseStateMachine, IHealth, IChokeDamageSource, IPlayerBody
    {
        public float CurrentHealth => _health;
        public float MaxHealth => _maxHealth;
        public float ChokeDamage => _chokeDamage;
        public bool IsAlive => !dead;

        public PlayerState PreviousState { get; set; }

        public PlayerRunState RunState { get; set; }
        public PlayerInvunerableState InvunerableState { get; set; }
        public PlayerWalkState WalkState { get; set; }
        public PlayerAttackState AttackState { get; set; }
        public PlayerDeadState DeadState { get; set; }
        public PlayerDodgeState DodgeState { get; set; }
        public PlayerRespawnState RespawnState { get; set; }
        public PlayerIdleState IdleState { get; set; }
        public PlayerJumpState JumpState { get; set; }

        [Header("Casting")]
        [Tooltip("Optional. Leave empty and the raid's voice casting (hold V) is used instead. " +
                 "See docs/systems/spells.md, \"Two ways to cast\".")]
        [SerializeField] private SpellBook _spellBook;

        /// <summary>
        /// The held spellbook, or null when the player casts by voice. Backed by a serialized field
        /// so it can be assigned in the Inspector — an auto-property cannot be, which is why this
        /// slot never appeared.
        /// </summary>
        public SpellBook SpellBook
        {
            get => _spellBook;
            set => _spellBook = value;
        }

        public Vector2 MovementDirection { get; set; }

        public Rigidbody _rb;

        public float walkSpeed;
        public float JumpForce;
        public float FallMultiplier = 2.5f;

        [Range(0, 1)]
        public float AirControl = 0.7f;

        public float DodgeForce;
        public float respawnSpeed;

        public float MouseSensitivity = 100f;
        public Transform CameraTransform;

        [Header("Ground Check Settings")]
        [SerializeField] private float _groundCheckRadius = 0.3f;
        [SerializeField] private float _groundCheckDistance = 1.6f;
        [SerializeField] private float _groundedHeight = 1.0f;
        [SerializeField] private float _groundSnapSpeed = 15f;
        [SerializeField] private LayerMask _groundLayer;
        public bool IsGrounded { get; private set; }

        [Header("Physics Damage Settings")]
        public float MinVelocityForDamage = 5f;

        [Header("Item Interaction Settings")]
        [SerializeField] private float _chokeDamage = 5f; // Damage per second while holding an enemy

        private float _xRotation = 0f;
        private float _yaw = 0f;
        private float _health;
        private float _maxHealth = 100;
        public bool dead;

        public override void ChangeState(IState newState)
        {
            if (newState == CurrentState)
                return;

            PreviousState = CurrentState as PlayerState;
            base.ChangeState(newState);
        }

        public void Look(Vector2 lookDelta)
        {
            if (CameraTransform == null)
            {
                Debug.LogWarning("CameraTransform is not assigned in PlayerStateMachine.");
                return;
            }

            float mouseX = lookDelta.x * MouseSensitivity * Time.deltaTime;
            float mouseY = lookDelta.y * MouseSensitivity * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
            _yaw += mouseX;

            // Yaw turns the camera, not the body. The body is an interpolated rigidbody (so the view
            // moves every rendered frame, not only on 50 Hz physics steps — issue #104), and
            // interpolation overwrites any rotation set on its transform between steps. Movement,
            // aiming, spells and melee all read the camera, so the capsule never needs to face anywhere.
            CameraTransform.localRotation = Quaternion.Euler(_xRotation, _yaw, 0f);
        }

        public void Awake()
        {
            RunState = new PlayerRunState(this);
            InvunerableState = new PlayerInvunerableState(this);
            WalkState = new PlayerWalkState(this);
            AttackState = new PlayerAttackState(this);
            DeadState = new PlayerDeadState(this);
            DodgeState = new PlayerDodgeState(this);
            RespawnState = new PlayerRespawnState(this);
            IdleState = new PlayerIdleState(this);
            JumpState = new PlayerJumpState(this);
            _rb = GetComponent<Rigidbody>();

            // Standard world gravity: let the physics engine apply Physics.gravity (-Y). Rotation
            // stays owned by look input only, so freeze it here (previously done by GravityReceiver).
            // freezeRotation is load-bearing: Player.prefab serialises m_Constraints: 0, so without
            // this line the capsule tips over and rolls the first time it touches anything.
            _rb.useGravity = true;
            _rb.freezeRotation = true;

            // Without this the camera (a child of this body) only moves on physics steps: at a high
            // frame rate the whole view judders at 50 Hz while held items glide, which reads as the
            // items and enemies lagging and smearing (#104).
            _rb.interpolation = RigidbodyInterpolation.Interpolate;

            _health = _maxHealth;
        }

        private void Start()
        {
            ChangeState(IdleState);
            AssignSpellBook(null);

            Camera view = CameraTransform != null ? CameraTransform.GetComponent<Camera>() : null;
            if (Local == null && view != null && view.enabled)
                Local = this;

            Plunderspell.Core.GameServices.Initialize();
            Plunderspell.Core.GameServices.GameState.StateChanged += OnGameStateChanged;
            PublishHealth();
        }

        /// <summary>
        /// Resolves the spellbook: an explicit one, else one already assigned in the Inspector, else
        /// one on this object. Null is a valid outcome and means "this player casts by voice".
        /// </summary>
        public void AssignSpellBook(SpellBook spellBook)
        {
            if (spellBook != null)
            {
                _spellBook = spellBook;
                return;
            }

            // Only fall back to a sibling component when nothing was authored, so Start() cannot
            // wipe an Inspector assignment the moment the scene loads.
            if (_spellBook == null)
            {
                _spellBook = GetComponent<SpellBook>();
            }
        }

        /// <summary>The player this machine renders through (its camera is live). Null until one exists.</summary>
        public static PlayerStateMachine Local { get; private set; }

        /// <summary>Raised when the local player dies. The raid treats it as a lost raid.</summary>
        public static event System.Action LocalPlayerDied;

        public bool IsLocal => Local == this;

        public void Die()
        {
            dead = true;
            ChangeState(DeadState);
            if (IsLocal)
            {
                LocalPlayerDied?.Invoke();
                if (Plunderspell.Core.GameServices.GameState != null)
                    Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.GameOver);
            }
        }

        /// <summary>Keeps the HUD's health number in step with the body's.</summary>
        private void PublishHealth()
        {
            if (IsLocal && Plunderspell.Core.GameServices.PlayerStats != null)
                Plunderspell.Core.GameServices.PlayerStats.SetHealth(Mathf.CeilToInt(Mathf.Max(0f, _health)));
        }

        /// <summary>Setting out again after dying: a fresh body, full health.</summary>
        private void OnGameStateChanged(Plunderspell.Core.GameState previous, Plunderspell.Core.GameState next)
        {
            if (next == Plunderspell.Core.GameState.Playing && previous == Plunderspell.Core.GameState.Lair && dead)
                ReviveTo(1f);
        }

        private void OnDestroy()
        {
            if (Plunderspell.Core.GameServices.GameState != null)
                Plunderspell.Core.GameServices.GameState.StateChanged -= OnGameStateChanged;
            if (Local == this)
                Local = null;
        }

        public void Walk()
        {
            ChangeState(WalkState);
        }

        /// <summary>
        /// Lair-revival entry point used by DownedPlayerCarryAdapter once a downed player has been
        /// carried to extraction. Clears the dead flag, restores health to the given fraction of max
        /// (0..1) and returns the player to a controllable state.
        /// </summary>
        public void ReviveTo(float healthFraction)
        {
            healthFraction = Mathf.Clamp01(healthFraction);
            _health = _maxHealth * healthFraction;
            dead = false;
            // Idle, not RespawnState: that state's exit ran on a thread-pool task and never landed.
            ChangeState(IdleState);
            PublishHealth();
        }

        public void TakeDamage(float damage)
        {
            if (dead) return;
            _health -= damage;
            PublishHealth();
            if (_health <= 0)
            {
                Die();
            }
        }

        public void TakeDamage(float damage, float impactVelocity)
        {
            if (dead) return;
            if (impactVelocity < MinVelocityForDamage) return;

            _health -= damage;
            PublishHealth();
            if (_health <= 0)
            {
                Die();
            }
        }

        public void Move(Vector2 movement)
        {
            MovementDirection = movement;
        }

        public void Run()
        {
            ChangeState(RunState);
        }

        public void Dodge()
        {
            if (CurrentState != DodgeState)
            {
                ChangeState(DodgeState);
            }
        }

        public void Respawn()
        {
            ChangeState(RespawnState);
        }

        public override void FixedUpdate()
        {
            GroundCheck();
            base.FixedUpdate();
            ApplyExtraFallGravity();
        }

        // Ground check against world up: a SphereCast along -Y, with proportional
        // error-correction snapping the body to the target ride height. This is what removes the
        // camera jitter a hard position snap would cause - ported from the old transform-based
        // controller's UpdateGravity.
        private void GroundCheck()
        {
            Vector3 up = Vector3.up;
            Vector3 origin = transform.position + up * 0.5f;
            float verticalSpeed = Vector3.Dot(_rb.linearVelocity, up);

            bool hitGround = Physics.SphereCast(origin, _groundCheckRadius, -up, out RaycastHit hit, _groundCheckDistance, _groundLayer);
            IsGrounded = hitGround;

            // Only snap while not actively rising - snapping mid-jump would cancel the jump.
            if (hitGround && verticalSpeed <= 0f)
            {
                CancelVelocityAlongUp(up, verticalSpeed);
                SnapToGroundHeight(hit, up);
            }
        }

        // Rigidbody gravity integrates downward speed every FixedUpdate regardless of contact, so
        // without this the downward speed grows without bound while SnapToGroundHeight holds the
        // position - the body fights itself hard enough to read as continuous jumping. Zeroing the
        // vertical (along-up) velocity component on every grounded frame keeps it planted.
        private void CancelVelocityAlongUp(Vector3 up, float verticalSpeed)
        {
            _rb.linearVelocity -= up * verticalSpeed;
        }

        private void SnapToGroundHeight(RaycastHit hit, Vector3 up)
        {
            float currentHeight = hit.distance - 0.5f;
            float error = currentHeight - _groundedHeight;
            if (Mathf.Abs(error) <= 0.001f) return;

            float correction = error * _groundSnapSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(correction) > Mathf.Abs(error)) correction = error;

            _rb.MovePosition(_rb.position - up * correction);
        }

        // Extra weight while falling, expressed as additional force along world gravity
        // (Physics.gravity, -Y) on top of the Rigidbody's built-in gravity, for a snappier arc.
        private void ApplyExtraFallGravity()
        {
            if (IsGrounded) return;

            _rb.AddForce(Physics.gravity * (FallMultiplier - 1f), ForceMode.Acceleration);
        }

        public void Dead()
        {
            ChangeState(DeadState);
        }

        public void Idle()
        {
            ChangeState(IdleState);
        }

        public void Jump()
        {
            if (IsGrounded && CurrentState != JumpState)
            {
                ChangeState(JumpState);
            }
        }

        public void Attack()
        {
            ChangeState(AttackState);

            // A player with no SpellBook is a valid setup (the test scene has one), so an unarmed
            // attack swings whatever is currently held instead of casting.
            if (SpellBook != null)
            {
                SpellBook.CastSpell();
            }
            else if (ItemManager.Instance != null && CameraTransform != null)
            {
                if (!ItemManager.Instance.TryMeleeSwing(CameraTransform.position, CameraTransform.forward))
                {
                    ItemManager.Instance.TryFireRanged(CameraTransform.position, CameraTransform.forward);
                }
            }
        }

        public void Invunerable()
        {
            ChangeState(InvunerableState);
        }
    }
}

using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Playtest
{
    /// <summary>
    /// A minimal first-person controller for playtesting: WASD, mouse look, jump, and a crouch/sprint
    /// stance that feeds the footstep noise the whole stealth game runs on.
    ///
    /// A harness, not the shipping controller: this is used by ItemGym.unity only. RaidScene carries
    /// <c>PlayerStateMachine</c> plus <c>PlayerInputController</c>, so a behaviour change made here
    /// does not reach the raid — see docs/6-decisions/Decisions.md, "Issue 9's gate belongs on the raid's player,
    /// not only on the playtest harness".
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FreeLookPlaytestController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _sprintSpeed = 7f;
        [SerializeField] private float _crouchSpeed = 1.8f;
        [SerializeField] private float _jumpSpeed = 5f;

        [Header("Look")]
        [SerializeField] private float _mouseSensitivity = 2.5f;
        [SerializeField] private float _maxPitch = 85f;

        [Header("Keys")]
        [SerializeField] private KeyCode _sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode _crouchKey = KeyCode.LeftControl;
        [SerializeField] private KeyCode _jumpKey = KeyCode.Space;

        [Header("Noise")]
        [Tooltip("Emits footsteps. Found on this GameObject when left empty.")]
        [SerializeField] private FootstepNoiseEmitter _footsteps;

        [Tooltip("Metres walked between footstep noises.")]
        [SerializeField] private float _stepDistance = 2f;

        private Rigidbody _body;
        private Transform _eye;
        private float _pitch;
        private float _distanceSinceStep;

        /// <summary>The stance the player is moving in, which decides how loud their footsteps are.</summary>
        public MoveStance Stance { get; private set; } = MoveStance.Walk;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.freezeRotation = true;

            if (_footsteps == null)
                _footsteps = GetComponent<FootstepNoiseEmitter>();

            Camera camera = GetComponentInChildren<Camera>();
            _eye = camera != null ? camera.transform : transform;
        }

        private void Update()
        {
            // Gated on the state, not by disabling this component: a component that is switched off
            // and on again loses the camera and rigidbody it resolved in Awake.
            if (!Plunderspell.Core.GameServices.IsPlaying)
                return;

            Look();
            UpdateStance();
        }

        private void FixedUpdate()
        {
            if (!Plunderspell.Core.GameServices.IsPlaying)
                return;

            Move();
        }

        private void Look()
        {
            float yaw = Input.GetAxis("Mouse X") * _mouseSensitivity;
            float pitchDelta = Input.GetAxis("Mouse Y") * _mouseSensitivity;

            transform.Rotate(0f, yaw, 0f);

            _pitch = Mathf.Clamp(_pitch - pitchDelta, -_maxPitch, _maxPitch);
            if (_eye != null)
                _eye.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateStance()
        {
            if (Input.GetKey(_crouchKey))
                Stance = MoveStance.Crouch;
            else if (Input.GetKey(_sprintKey))
                Stance = MoveStance.Run;
            else
                Stance = MoveStance.Walk;
        }

        private void Move()
        {
            Vector3 wish = transform.right * Input.GetAxisRaw("Horizontal") +
                           transform.forward * Input.GetAxisRaw("Vertical");
            if (wish.sqrMagnitude > 1f)
                wish = wish.normalized;

            float speed = SpeedFor(Stance);
            Vector3 velocity = _body.linearVelocity;
            Vector3 target = wish * speed;

            // Horizontal velocity is driven directly; vertical is left to gravity and the jump.
            _body.linearVelocity = new Vector3(target.x, velocity.y, target.z);

            if (Input.GetKey(_jumpKey) && Mathf.Abs(velocity.y) < 0.01f)
                _body.linearVelocity = new Vector3(target.x, _jumpSpeed, target.z);

            AccumulateFootsteps(target, Time.fixedDeltaTime);
        }

        private float SpeedFor(MoveStance stance)
        {
            switch (stance)
            {
                case MoveStance.Crouch: return _crouchSpeed;
                case MoveStance.Run: return _sprintSpeed;
                default: return _walkSpeed;
            }
        }

        /// <summary>
        /// Emits a footstep every <see cref="_stepDistance"/> metres travelled rather than on a timer,
        /// so crossing a room makes the same number of noises however fast you cross it — sprinting is
        /// louder per step, not more steps.
        /// </summary>
        private void AccumulateFootsteps(Vector3 velocity, float deltaTime)
        {
            if (_footsteps == null)
                return;

            var horizontal = new Vector3(velocity.x, 0f, velocity.z);
            _distanceSinceStep += horizontal.magnitude * deltaTime;

            if (_distanceSinceStep < _stepDistance)
                return;

            _distanceSinceStep = 0f;
            _footsteps.OnFootstep(Stance);
        }
    }
}

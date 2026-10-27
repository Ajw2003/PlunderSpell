using Plunderspell.Voice;
using StateMachine;
using UnityEngine;

namespace Player
{
    // doc-ref 7a1c docs/4-systems/player-animation.md
    /// <summary>
    /// Plays the wizard's clips from what the player is doing: Animator parameters only, no
    /// gameplay reads from it. Remote flags are copied in by <c>PlayerNetworkOwnership</c>.
    /// </summary>
    public class WizardAnimationDriver : MonoBehaviour
    {
        public static readonly int SpeedParam = Animator.StringToHash("Speed");
        public static readonly int CrouchParam = Animator.StringToHash("Crouch");
        public static readonly int AirborneParam = Animator.StringToHash("Airborne");
        public static readonly int CastingParam = Animator.StringToHash("Casting");
        public static readonly int DeadParam = Animator.StringToHash("Dead");

        [SerializeField] private Animator _animator;
        [SerializeField] private PlayerStateMachine _body;
        [SerializeField] private PushToCastController _cast;

        [Tooltip("How quickly the measured speed follows the body, per second. Higher is twitchier.")]
        [SerializeField] private float _speedSmoothing = 12f;

        [Tooltip("Seconds the collapse plays before the body is hidden (death_collapse is 1.5 s).")]
        [SerializeField] private float _vanishAfterSeconds = 1.6f;

        private Renderer[] _renderers;
        private Vector3 _lastPosition;
        private float _speed;
        private bool _isDown;
        private float _downSince;
        private bool _shadowOnly;

        /// <summary>True on a body another machine owns: the three flags below are used instead of
        /// reading this machine's input. Set by the network layer.</summary>
        public bool IsRemote { get; set; }

        public bool RemoteCreeping { get; set; }
        public bool RemoteCasting { get; set; }
        public bool RemoteAirborne { get; set; }
        public bool RemoteDown { get; set; }

        /// <summary>Crouching (the creep key) as this machine sees it.</summary>
        public bool Creeping => IsRemote ? RemoteCreeping : _body != null && _body.Creeping;

        /// <summary>Holding the cast key as this machine sees it.</summary>
        public bool Casting => IsRemote ? RemoteCasting : _cast != null && _cast.IsCasting;

        /// <summary>Off the ground as this machine sees it.</summary>
        public bool Airborne => IsRemote ? RemoteAirborne : _body != null && !_body.IsGrounded;

        /// <summary>The wizard's model root; turned to face where the player looks.</summary>
        public Transform Model => _animator != null ? _animator.transform : null;

        private void Awake()
        {
            if (_body == null) _body = GetComponent<PlayerStateMachine>();
            if (_cast == null) _cast = GetComponent<PushToCastController>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator == null)
                Debug.LogError($"[Wizard] {name}: no Animator under the player; the wizard will not animate. " +
                               "Run Plunderspell > Wizard > Install On Player Prefabs.", this);
            _renderers = _animator != null ? _animator.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (_animator == null)
                return;

            UpdateOwnView();
            FaceLookDirection();

            float dt = Mathf.Max(Time.deltaTime, 1e-5f);
            Vector3 moved = transform.position - _lastPosition;
            _lastPosition = transform.position;
            moved.y = 0f;
            float measured = moved.magnitude / dt;
            _speed = Mathf.Lerp(_speed, measured, 1f - Mathf.Exp(-_speedSmoothing * dt));

            bool down = IsDown();
            if (down != _isDown)
            {
                _isDown = down;
                _downSince = Time.time;
                SetVisible(true);
            }
            if (_isDown && Time.time - _downSince >= _vanishAfterSeconds)
                SetVisible(false);

            _animator.SetFloat(SpeedParam, _speed);
            _animator.SetBool(CrouchParam, Creeping);
            _animator.SetBool(AirborneParam, Airborne && !down);
            _animator.SetBool(CastingParam, Casting && !down);
            _animator.SetBool(DeadParam, down);
        }

        private bool IsDown() => IsRemote ? RemoteDown : _body != null && _body.dead;

        // The body itself never turns: look input yaws the camera (PlayerStateMachine.Look), and the
        // camera's transform is replicated, so the model copies its yaw on every machine.
        private void FaceLookDirection()
        {
            if (_body == null || _body.CameraTransform == null || _isDown)
                return;
            Vector3 forward = Vector3.ProjectOnPlane(_body.CameraTransform.forward, Vector3.up);
            if (forward.sqrMagnitude > 1e-4f)
                Model.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        // Your own body is in front of your own camera: draw it as a shadow only, so you see your
        // shadow but not the inside of your own hat. Everyone else sees the whole wizard.
        private void UpdateOwnView()
        {
            bool own = !IsRemote && _body != null && _body.IsLocal;
            if (own == _shadowOnly)
                return;
            _shadowOnly = own;
            var mode = own ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                           : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (Renderer r in _renderers)
                if (r != null) r.shadowCastingMode = mode;
        }

        // ponytail: the whole wizard hides, hat included; #339 drops a separate hat where they fell.
        private void SetVisible(bool visible)
        {
            foreach (Renderer r in _renderers)
                if (r != null && r.enabled != visible) r.enabled = visible;
        }
    }
}

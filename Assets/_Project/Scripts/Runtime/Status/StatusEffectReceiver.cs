using System;
using Interfaces;
using UnityEngine;

namespace RogueAi.Status
{
    /// <summary>
    /// One component that makes anything a valid target for the status-inflicting spells: burning
    /// (Ignis), stun (Tonitrus), sleep (Somnus) and levitation (Levo). Attach it to players, guards
    /// and anything else a spell should be able to affect.
    ///
    /// Why one component rather than an implementation per actor: the spell layer must not know what
    /// it hit, and actors must not each re-derive "how long am I stunned for". Timers, stacking rules
    /// and expiry live here once; actors read <see cref="IsIncapacitated"/> and stop acting.
    ///
    /// Stacking rule, applied uniformly: a new application takes the LONGER of the remaining and the
    /// new duration, never the sum. Two guards both casting Somnus should not sleep a player for
    /// sixteen seconds.
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusEffectReceiver : MonoBehaviour, IIgnitable, IStunnable, ISleepable, ILevitatable
    {
        [Header("Wiring (optional — resolved from this GameObject when left empty)")]
        [Tooltip("Health that burn damage is applied to.")]
        [SerializeField] private MonoBehaviour _healthSource;

        [Tooltip("Body that levitation impulses are applied to.")]
        [SerializeField] private Rigidbody _body;

        private IHealth _health;
        private bool _healthResolved;

        private float _burnRemaining;
        private float _burnDps;
        private GameObject _burnInstigator;
        private float _stunRemaining;
        private float _sleepRemaining;
        private float _levitateRemaining;

        /// <summary>Damage not yet applied, carried between frames so a partial second still hurts.</summary>
        private float _pendingBurnDamage;

        public bool IsBurning => _burnRemaining > 0f;
        public bool IsStunned => _stunRemaining > 0f;
        public bool IsAsleep => _sleepRemaining > 0f;
        public bool IsLevitating => _levitateRemaining > 0f;

        /// <summary>True while the actor cannot act — asleep or stunned. AI and input check this.</summary>
        // Floating counts: a guard held in the air by Levo cannot see, chase or strike.
        public bool IsIncapacitated => IsStunned || IsAsleep || IsLevitating;

        public float BurnRemaining => _burnRemaining;
        public float StunRemaining => _stunRemaining;
        public float SleepRemaining => _sleepRemaining;
        public float LevitateRemaining => _levitateRemaining;

        /// <summary>Raised when any status starts or expires, so AI and UI can react without polling.</summary>
        public event Action<StatusEffectReceiver> StatusChanged;

        private void Awake()
        {
            if (_body == null)
                _body = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// The health that burn damage lands on, resolved on first use rather than in Awake.
        ///
        /// This must stay lazy. A [RequireComponent(typeof(StatusEffectReceiver))] actor — a guard,
        /// say — causes this component to be added FIRST, so its Awake runs before the actor's own
        /// component exists. Resolving in Awake finds nothing and the actor silently never takes
        /// burn damage.
        /// </summary>
        private IHealth Health
        {
            get
            {
                if (_healthResolved && _health != null)
                    return _health;

                _health = _healthSource as IHealth ?? GetComponent<IHealth>();
                _healthResolved = _health != null;
                return _health;
            }
        }

        private void Update() => Tick(Time.deltaTime);

        /// <summary>
        /// Advances every timer by <paramref name="deltaTime"/> and applies burn damage. Public and
        /// network-free so tests drive it directly instead of waiting for real seconds to pass.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            bool changed = false;

            if (_burnRemaining > 0f)
            {
                float burnt = Mathf.Min(deltaTime, _burnRemaining);
                _pendingBurnDamage += _burnDps * burnt;
                _burnRemaining -= deltaTime;

                // Apply in whole points so a 0.016s frame does not spam sub-pixel damage events.
                IHealth health = Health;
                if (_pendingBurnDamage >= 1f && health != null)
                {
                    float toApply = Mathf.Floor(_pendingBurnDamage);
                    _pendingBurnDamage -= toApply;
                    Damage.Apply(health, toApply, gameObject, _burnInstigator,
                        transform.position + Vector3.up, DamageKind.Burn);
                }

                if (_burnRemaining <= 0f)
                {
                    _burnRemaining = 0f;
                    _burnDps = 0f;
                    changed = true;
                }
            }

            changed |= CountDown(ref _stunRemaining, deltaTime);
            changed |= CountDown(ref _sleepRemaining, deltaTime);
            changed |= CountDown(ref _levitateRemaining, deltaTime);

            if (changed)
                StatusChanged?.Invoke(this);
        }

        private static bool CountDown(ref float remaining, float deltaTime)
        {
            if (remaining <= 0f)
                return false;
            remaining -= deltaTime;
            if (remaining > 0f)
                return false;
            remaining = 0f;
            return true;
        }

        // --- IIgnitable ------------------------------------------------------------------------

        public void Ignite(float damagePerSecond, float duration, GameObject instigator = null)
        {
            if (damagePerSecond <= 0f || duration <= 0f)
                return;

            _burnInstigator = instigator;

            // Hotter fire wins on rate; longer fire wins on duration. Neither stacks additively.
            _burnDps = Mathf.Max(_burnDps, damagePerSecond);
            _burnRemaining = Mathf.Max(_burnRemaining, duration);
            StatusChanged?.Invoke(this);
        }

        /// <summary>Put the fire out (a water spell, or wading into the moat).</summary>
        public void Extinguish()
        {
            if (!IsBurning)
                return;
            _burnRemaining = 0f;
            _burnDps = 0f;
            _pendingBurnDamage = 0f;
            StatusChanged?.Invoke(this);
        }

        // --- IStunnable ------------------------------------------------------------------------

        public void Stun(float duration)
        {
            if (duration <= 0f)
                return;
            _stunRemaining = Mathf.Max(_stunRemaining, duration);
            StatusChanged?.Invoke(this);
        }

        // --- ISleepable ------------------------------------------------------------------------

        public void Sleep(float duration)
        {
            if (duration <= 0f)
                return;
            _sleepRemaining = Mathf.Max(_sleepRemaining, duration);
            StatusChanged?.Invoke(this);
        }

        /// <summary>
        /// Wakes the sleeper. Loud noise nearby calls this — sleeping guards are a delay, not a
        /// removal, which is what keeps Somnus from trivialising a patrol.
        /// </summary>
        public void WakeUp()
        {
            if (!IsAsleep)
                return;
            _sleepRemaining = 0f;
            StatusChanged?.Invoke(this);
        }

        // --- ILevitatable ----------------------------------------------------------------------

        /// <summary>Who most recently levitated this, for blame when it falls.</summary>
        public GameObject LevitatedBy { get; private set; }

        public void Levitate(Vector3 impulse, float duration, GameObject instigator = null)
        {
            if (duration <= 0f)
                return;

            LevitatedBy = instigator;

            _levitateRemaining = Mathf.Max(_levitateRemaining, duration);

            if (_body != null && !_body.isKinematic)
                _body.AddForce(impulse, ForceMode.VelocityChange);

            StatusChanged?.Invoke(this);
        }

        /// <summary>Clears every status. Used on respawn and between raids.</summary>
        public void ClearAll()
        {
            bool had = IsBurning || IsStunned || IsAsleep || IsLevitating;
            _burnRemaining = _burnDps = _pendingBurnDamage = 0f;
            _stunRemaining = _sleepRemaining = _levitateRemaining = 0f;
            if (had)
                StatusChanged?.Invoke(this);
        }
    }
}

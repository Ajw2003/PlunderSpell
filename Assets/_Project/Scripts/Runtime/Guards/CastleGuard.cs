using System;
using System.Collections.Generic;
using Interfaces;
using PurrNet;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Status;
using UnityEngine;
using UnityEngine.AI;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A castle guard: the thing that makes noise matter.
    ///
    /// It hears (<see cref="INoiseListener"/>), it sees (a cone check with a line-of-sight raycast),
    /// and it can be shut down by the spells that target the living — Somnus, Frango and Ignis all
    /// reach it through <see cref="StatusEffectReceiver"/>. Every decision it makes is delegated to
    /// <see cref="GuardBrain"/>, so its behaviour is asserted in tests rather than observed in play.
    ///
    /// Server-authoritative: only the server ticks the AI and moves the agent. Clients see the
    /// replicated transform and the replicated alert state.
    /// </summary>
    [RequireComponent(typeof(StatusEffectReceiver))]
    public class CastleGuard : NetworkBehaviour, INoiseListener, IEavesdropper, IHealth, IShovable
    {
        [Header("Senses")]
        [Tooltip("How far this guard can see while the castle is calm, in metres.")]
        [SerializeField] private float _sightRange = 14f;

        [Tooltip("Field of view in degrees.")]
        [SerializeField] private float _fieldOfView = 110f;

        [Tooltip("Eye height above the guard's pivot, in metres.")]
        [SerializeField] private float _eyeHeight = 1.6f;

        [Tooltip("Layers that block line of sight.")]
        [SerializeField] private LayerMask _geometryLayers;

        [Header("Movement")]
        [SerializeField] private float _patrolSpeed = 2.0f;
        [SerializeField] private float _chaseSpeed = 4.5f;

        [Tooltip("How close counts as having reached a destination, in metres.")]
        [SerializeField] private float _arrivalDistance = 1.0f;

        [Tooltip("Degrees per second the guard turns toward where it is heading.")]
        [SerializeField] private float _turnSpeed = 240f;

        [Header("Patrol")]
        [Tooltip("Points walked in order. With fewer than two, the guard stands its post.")]
        [SerializeField] private List<Transform> _patrolRoute = new List<Transform>();

        [Header("Attack")]
        [Tooltip("How close this guard must be to strike, in metres. Ignored when it fires instead.")]
        [SerializeField] private float _attackRange = 2.0f;

        [Tooltip("Damage per hit.")]
        [SerializeField] private float _attackDamage = 12f;

        [Tooltip("Seconds between attacks.")]
        [SerializeField] private float _attackCooldown = 1.4f;

        [Tooltip("Leave empty for a melee guard. Assign a projectile and this guard shoots instead, " +
                 "at its sight range rather than its attack range — this is what makes a turret a turret.")]
        [SerializeField] private GameObject _projectilePrefab;

        [Tooltip("Speed the projectile leaves at, in metres per second.")]
        [SerializeField] private float _projectileSpeed = 18f;

        [Header("Health")]
        [SerializeField] private float _maxHealth = 100f;

        [Header("Alarm")]
        [Tooltip("The castle alarm. Found in the scene when left empty.")]
        [SerializeField] private EnemyDirector _alarm;

        [Tooltip("Noise this guard makes when it spots an intruder and raises the cry.")]
        [Range(0f, 1f)] [SerializeField] private float _shoutStrength = 0.8f;
        [SerializeField] private float _shoutRadius = 20f;

        private readonly SyncVar<GuardAlertState> _state =
            new SyncVar<GuardAlertState>(GuardAlertState.Patrolling);

        [field: SerializeField] private SyncVar<float> _health { get; set; } = new SyncVar<float>(100f);

        // Every attack bumps this, so every peer learns a guard swung or fired, not only the server
        // that resolved the hit. Packed count + kind; see GuardAttackSignal and docs/4-systems/net.md.
        private readonly SyncVar<int> _attackSignal = new SyncVar<int>(GuardAttackSignal.None);

        private StatusEffectReceiver _status;
        private NavMeshAgent _agent;
        private Rigidbody _body;
        private bool _bodyDriven;
        private Vector3 _shoveVelocity;
        private float _shoveUntil;
        private const float k_bodyMass = 80f;
        private const float k_shoveSeconds = 0.25f;

        private Vector3? _investigationTarget;
        private Vector3 _lastKnownIntruderPosition;
        private float _timeSinceLastContact;
        private int _patrolIndex;
        private bool _hasShoutedThisChase;
        private float _lastAttackTime = float.NegativeInfinity;

        // Never standing still (#193). The reasoning is in docs/4-systems/raid.md, "Guards that keep moving".
        private float _tickDelta;
        private bool _seenThisTick;
        private float _lookTimeLeft;
        private float _huntClock;
        private Vector3 _sweepCentre;
        private int _sweepIndex;
        private Vector3? _sweepGoal;
        private Vector3? _wanderGoal;
        private Vector3? _home;

        /// <summary>What this guard is currently doing.</summary>
        public GuardAlertState State => _state.value;

        [field: SerializeField] public float CurrentHealth => _health.value;
        public float MaxHealth => _maxHealth;

        /// <summary>True when asleep, stunned or otherwise unable to act.</summary>
        public bool IsIncapacitated => _status != null && _status.IsIncapacitated;

        /// <summary>Where the guard is heading, when it has somewhere to be.</summary>
        public Vector3? Destination { get; private set; }

        /// <summary>Raised on this peer whenever the guard changes what it is doing.</summary>
        public event Action<GuardAlertState> StateChanged;

        /// <summary>
        /// Raised on every peer each time this guard attacks: on the server as the attack happens,
        /// on a client when the replicated signal arrives. What an animator or a sound hooks.
        /// </summary>
        public event Action<GuardAttackKind> Attacked;

        /// <summary>How many times this guard has attacked, as far as this peer has heard.</summary>
        public int AttackCount => GuardAttackSignal.Count(_attackSignal.value);

        /// <summary>The kind of the most recent attack. Meaningless while <see cref="AttackCount"/> is 0.</summary>
        public GuardAttackKind LastAttackKind => GuardAttackSignal.Kind(_attackSignal.value);

        /// <summary>The intruders this guard is watching for, from its director (#205). Empty without one.</summary>
        private IReadOnlyList<Transform> Intruders => EnemyDirector.IntrudersOf(_alarm);

        private void Awake()
        {
            _status = GetComponent<StatusEffectReceiver>();
            _agent = GetComponent<NavMeshAgent>();

            // The guard is a real physics participant (#200): a finite-mass dynamic body the agent only
            // plans for. It starts kinematic; the server flips it dynamic in FixedUpdate (see
            // FixedUpdate), a client leaves it kinematic under the replicated transform.
            if (_agent != null)
            {
                if (!TryGetComponent(out _body))
                    _body = gameObject.AddComponent<Rigidbody>();
                _body.mass = k_bodyMass;
                _body.freezeRotation = true;
                _body.useGravity = true;
                _body.interpolation = RigidbodyInterpolation.Interpolate;
                _body.isKinematic = true;
                foreach (Collider c in GetComponentsInChildren<Collider>())
                    if (!c.isTrigger)
                        c.sharedMaterial = Slippery;
            }
            _health.value = _maxHealth;

            if (_alarm == null)
                WatchAlarm(EnemyDirector.Current != null ? EnemyDirector.Current : FindObjectOfType<EnemyDirector>());
        }

        private void OnEnable()
        {
            _alarm?.RegisterGuard(this);
            IgnoreOtherGuards();
        }

        /// <summary>
        /// Guards pass through each other (as their kinematic bodies always did) so two dynamic bodies
        /// cannot jam, while still colliding with players and walls (#200).
        /// </summary>
        private void IgnoreOtherGuards()
        {
            Collider[] mine = GetComponentsInChildren<Collider>();
            IReadOnlyList<Component> guards = EnemyDirector.GuardsOf(_alarm);
            for (int g = 0; g < guards.Count; g++)
            {
                if (!(guards[g] is CastleGuard other) || other == this)
                    continue;
                foreach (Collider theirs in other.GetComponentsInChildren<Collider>())
                    foreach (Collider c in mine)
                        Physics.IgnoreCollision(c, theirs, true);
            }
        }

        private static PhysicsMaterial s_slippery;

        /// <summary>No friction against walls and floor: the body is steered by velocity, so friction only
        /// ever holds it against a wall it brushes (#200).</summary>
        private static PhysicsMaterial Slippery
        {
            get
            {
                if (s_slippery == null)
                    s_slippery = new PhysicsMaterial("GuardSlippery")
                    {
                        dynamicFriction = 0f,
                        staticFriction = 0f,
                        bounciness = 0f,
                        frictionCombine = PhysicsMaterialCombine.Minimum,
                        bounceCombine = PhysicsMaterialCombine.Minimum,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                return s_slippery;
            }
        }

        private void OnDisable()
        {
            _alarm?.UnregisterGuard(this);
        }

        protected override void OnDestroy()
        {
            WatchAlarm(null);
            base.OnDestroy();
        }

        // -----------------------------------------------------------------------------------------
        // Raising the castle (#163)
        // -----------------------------------------------------------------------------------------

        /// <summary>At the hue and cry, every guard this close to a player goes for that player.</summary>
        public const float HueAndCryRadius = 40f;

        /// <summary>The director this guard reports to and listens to. Null outside a raid.</summary>
        public EnemyDirector Alarm => _alarm;

        private void WatchAlarm(EnemyDirector director)
        {
            if (_alarm != null)
            {
                _alarm.OnInvestigateRequest -= OnInvestigateRequest;
                _alarm.UnregisterGuard(this);
            }
            _alarm = director;
            if (_alarm != null)
            {
                _alarm.OnInvestigateRequest += OnInvestigateRequest;
                if (isActiveAndEnabled)
                {
                    _alarm.RegisterGuard(this);
                    IgnoreOtherGuards();
                }
            }
        }

        private int _requestFrame = -1;
        private float _requestDistance;

        /// <summary>
        /// The director asks guards to investigate a spot (the hue and cry, #163/#205). This guard
        /// decides: it heads there only when the spot is within <see cref="HueAndCryRadius"/>, and when
        /// several players are asked about in one go it keeps the nearest.
        /// </summary>
        private void OnInvestigateRequest(InvestigateRequest request)
        {
            if (isSpawned && !isServer)
                return;
            float d = Vector3.Distance(transform.position, request.Position);
            if (d > HueAndCryRadius)
                return;
            if (_requestFrame == Time.frameCount && d >= _requestDistance)
                return;
            _requestFrame = Time.frameCount;
            _requestDistance = d;
            _huntClock = 0f;
            AlertTo(request.Position);
        }

        /// <summary>
        /// Sends this guard to <paramref name="position"/>, where an intruder was: it investigates
        /// there unless it is already chasing someone, and it does not wake from sleep or stun for
        /// it. Returns whether the guard took it up.
        /// </summary>
        public bool AlertTo(Vector3 position)
        {
            if (IsDead || IsIncapacitated || _state.value == GuardAlertState.Chasing)
                return false;
            _investigationTarget = position;
            _lookTimeLeft = 0f;
            if (_state.value != GuardAlertState.Investigating)
                EnterState(GuardAlertState.Investigating, _alarm != null ? _alarm.State : AlarmState.Calm);
            AlertsReceived++;
            return true;
        }

        /// <summary>How many times this guard has been sent after an intruder by a shout or the hue
        /// and cry, for tests and the live check.</summary>
        public int AlertsReceived { get; private set; }

        /// <summary>Sends every active guard within <paramref name="radius"/> of <paramref name="centre"/>,
        /// except <paramref name="except"/>, to <paramref name="target"/>. Returns how many went.</summary>
        private int AlertGuardsNear(Vector3 centre, float radius, Vector3 target, CastleGuard except = null)
        {
            int alerted = 0;
            IReadOnlyList<Component> guards = EnemyDirector.GuardsOf(_alarm);
            for (int i = guards.Count - 1; i >= 0; i--)
            {
                CastleGuard guard = guards[i] as CastleGuard;
                if (guard == null || guard == except)
                    continue;
                if (Vector3.Distance(guard.transform.position, centre) <= radius && guard.AlertTo(target))
                    alerted++;
            }
            return alerted;
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _attackSignal.onChanged += OnAttackSignalReplicated;
            // A client's guard is moved by the server's replicated transform. Its own agent would
            // fight that, and has no NavMesh to stand on until the client has built the castle.
            if (!isServer && _agent != null)
                _agent.enabled = false;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            _attackSignal.onChanged -= OnAttackSignalReplicated;
        }

        /// <summary>A client's view of an attack the server resolved. The server raised its own already.</summary>
        private void OnAttackSignalReplicated(int signal)
        {
            if (isServer || GuardAttackSignal.Count(signal) == 0)
                return;
            Attacked?.Invoke(GuardAttackSignal.Kind(signal));
        }

        /// <summary>
        /// Server only. Hands the guard to the physics: the agent plans (updatePosition off, so it never
        /// writes the transform, which is what made agent and physics fight in #104), and the body
        /// follows the agent's desired velocity. Because the body is dynamic with finite mass, a
        /// contact with a player or a wall stops it instead of crushing a player into the wall (#200).
        /// </summary>
        private void FixedUpdate()
        {
            if (_body == null || _agent == null || (isSpawned && !isServer))
                return;

            bool free = !_floating && !_falling && _agent.enabled && _agent.isOnNavMesh && _health.value > 0f;
            if (!free)
            {
                _bodyDriven = false;
                return;
            }

            if (!_bodyDriven)
            {
                _bodyDriven = true;
                _agent.updatePosition = false;
                _body.isKinematic = false;
                _body.linearVelocity = Vector3.zero;
                _agent.nextPosition = _body.position;
            }

            Vector3 want = _agent.isStopped ? Vector3.zero : _agent.desiredVelocity;
            want.y = 0f;
            if (Time.time < _shoveUntil)
                want += _shoveVelocity;
            Vector3 v = _body.linearVelocity;
            _body.linearVelocity = new Vector3(want.x, v.y, want.z);
            _agent.nextPosition = _body.position;
        }

        /// <summary>Knocks the guard back by <paramref name="metres"/> over a moment; walls and bodies stop it.</summary>
        public void Shove(Vector3 metres)
        {
            _shoveVelocity = new Vector3(metres.x, 0f, metres.z) / k_shoveSeconds;
            _shoveUntil = Time.time + k_shoveSeconds;
        }

        private void Update()
        {
            // Clients render what the server decided; only the server runs the AI.
            if (isSpawned && !isServer)
                return;
            if (UpdateLevitation(Time.deltaTime))
                return;
            Tick(Time.deltaTime);
        }

        // -----------------------------------------------------------------------------------------
        // Levitation (Levo)
        // -----------------------------------------------------------------------------------------

        private const float k_liftHeight = 1.8f;
        private const float k_liftSpeed = 3f;
        private const float k_fallDamagePerMetre = 9f;

        private bool _floating;
        private bool _falling;
        private float _floatBaseY;
        private float _fallFromY;
        private float _fallStartedAt;
        private float _fallSpeed;

        /// <summary>True while Levo holds this guard up or it is still falling back down.</summary>
        public bool IsAirborne => _floating || _falling;

        /// <summary>
        /// Lifts the guard while it levitates and drops it when the spell ends. The guard's body is
        /// kinematic (the NavMeshAgent owns its position), so an impulse cannot lift it: the agent is
        /// suspended and the transform raised directly, then the body falls under real gravity and
        /// takes damage for the height. Returns true while the AI must not run.
        /// </summary>
        public bool UpdateLevitation(float deltaTime)
        {
            bool levitating = _status != null && _status.IsLevitating;
            TryGetComponent(out Rigidbody body);

            if (levitating)
            {
                if (!_floating)
                {
                    _floating = true;
                    _falling = false;
                    _floatBaseY = transform.position.y;
                    if (_agent != null)
                    {
                        _agent.enabled = false;
                        _agent.updatePosition = true;
                    }
                    _bodyDriven = false;
                    if (body != null)
                    {
                        body.isKinematic = true;
                        body.freezeRotation = true;
                    }
                }

                Vector3 p = transform.position;
                p.y = Mathf.MoveTowards(p.y, _floatBaseY + k_liftHeight, k_liftSpeed * deltaTime);
                transform.position = p;
                transform.Rotate(Vector3.up, 45f * deltaTime, Space.World);
                return true;
            }

            if (_floating)
            {
                _floating = false;
                _falling = true;
                _fallFromY = transform.position.y;
                _fallStartedAt = Time.time;
                _fallSpeed = 0f;
                if (body != null)
                {
                    body.isKinematic = false;
                    body.useGravity = true;
                    body.freezeRotation = true;
                    body.linearVelocity = Vector3.zero;
                }
                return true;
            }

            if (_falling && body == null)
            {
                // Raid guards have no Rigidbody, so fall by hand, straight back down to where the lift
                // started: the guard was raised vertically with its agent off, so that is the floor.
                // Landing at once here cost the drop nothing (#106).
                _fallSpeed -= Physics.gravity.y * deltaTime;
                Vector3 p = transform.position;
                p.y = Mathf.Max(_floatBaseY, p.y - _fallSpeed * deltaTime);
                transform.position = p;
                if (p.y > _floatBaseY)
                    return true;
                Land(null);
                return false;
            }

            if (_falling)
            {
                bool settled = Time.time - _fallStartedAt > 0.25f && Mathf.Abs(body.linearVelocity.y) < 0.05f;
                bool timedOut = Time.time - _fallStartedAt > 3f;
                if (!settled && !timedOut)
                    return true;
                Land(body);
                return false;
            }

            return false;
        }

        private void Land(Rigidbody body)
        {
            _falling = false;
            if (body != null)
                body.isKinematic = true;

            float height = Mathf.Max(0f, _fallFromY - transform.position.y);
            if (height > 0.5f)
            {
                GameObject blame = _status != null ? _status.LevitatedBy : null;
                Damage.Apply(this, height * k_fallDamagePerMetre, gameObject, blame,
                    transform.position + Vector3.up * 0.2f, DamageKind.Impact);
            }

            _bodyDriven = false;
            if (_agent != null && _health.value > 0f)
            {
                _agent.updatePosition = true;
                _agent.enabled = true;
                if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                    _agent.Warp(hit.position);
            }
        }

        /// <summary>
        /// Advances the guard by <paramref name="deltaTime"/>: look, decide, move. Public and
        /// network-free so a test can step a guard through a situation without a scene running.
        /// </summary>
        public void Tick(float deltaTime)
        {
            AlarmState alarm = _alarm != null ? _alarm.State : AlarmState.Calm;
            _tickDelta = deltaTime;
            _home ??= transform.position;

            Transform seen = FindVisibleIntruder(alarm);
            _seenThisTick = seen != null;
            if (seen != null)
            {
                _lastKnownIntruderPosition = seen.position;
                _timeSinceLastContact = 0f;
            }
            else
            {
                _timeSinceLastContact += deltaTime;
            }

            GuardAlertState next = GuardBrain.NextState(
                _state.value,
                IsIncapacitated,
                seen != null,
                _investigationTarget.HasValue,
                _timeSinceLastContact,
                alarm);

            if (next != _state.value)
                EnterState(next, alarm);

            KeepHunting(next, alarm, deltaTime);
            Act(next, seen, alarm, deltaTime);
        }

        /// <summary>
        /// While the alarm is at the hue and cry, sends a searching or investigating guard toward a
        /// point near the nearest player every few seconds (#195). The alarm event sends each guard
        /// once; without this they reach that point, find nobody and stop.
        /// </summary>
        private void KeepHunting(GuardAlertState state, AlarmState alarm, float deltaTime)
        {
            if (!GuardBrain.ShouldHunt(state, alarm))
            {
                _huntClock = 0f;
                return;
            }

            _huntClock += deltaTime;
            if (!GuardBrain.HuntDue(_huntClock, GetInstanceID()))
                return;
            _huntClock = 0f;

            Transform nearest = NearestIntruder();
            if (nearest == null)
                return;

            Vector3 rough = nearest.position
                + GuardBrain.HuntOffset(UnityEngine.Random.value, UnityEngine.Random.value);
            rough = SnapToMesh(rough);

            if (state == GuardAlertState.Investigating)
            {
                _investigationTarget = rough;
                _lookTimeLeft = 0f;
            }
            else
            {
                FollowToward(rough);
            }
        }

        private Transform NearestIntruder()
        {
            Transform nearest = null;
            float best = float.MaxValue;
            IReadOnlyList<Transform> intruders = Intruders;
            for (int i = 0; i < intruders.Count; i++)
            {
                Transform intruder = intruders[i];
                if (intruder == null)
                    continue;
                float d = (intruder.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = intruder;
                }
            }
            return nearest;
        }

        /// <summary>Makes <paramref name="position"/> the last-known spot and restarts the sweep around
        /// it. A fresh lead also restarts the search patience.</summary>
        private void FollowToward(Vector3 position)
        {
            _lastKnownIntruderPosition = position;
            _timeSinceLastContact = 0f;
            ResetSweep();
        }

        private void ResetSweep()
        {
            _sweepCentre = _lastKnownIntruderPosition;
            _sweepIndex = 0;
            _sweepGoal = null;
        }

        // -----------------------------------------------------------------------------------------
        // Hearing
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// <see cref="INoiseListener"/>: a noise reached this guard. Loud enough noise also wakes a
        /// sleeping guard — Somnus buys time, it does not remove a patrol.
        /// </summary>
        public void OnNoiseHeard(NoiseEvent noise)
        {
            AlarmState alarm = _alarm != null ? _alarm.State : AlarmState.Calm;

            if (_status != null && _status.IsAsleep && noise.Strength >= NoiseWakeThreshold)
                _status.WakeUp();

            if (!GuardBrain.ShouldInvestigate(noise.Strength, alarm))
                return;

            // A hunting guard follows the newer noise (#194); one that can see its target ignores it.
            if (GuardBrain.ShouldFollowNoise(_state.value, _seenThisTick, noise.Strength, alarm))
            {
                FollowToward(noise.Origin);
                return;
            }

            // A louder noise overrides a quieter one already being walked toward.
            _investigationTarget = noise.Origin;
            _lookTimeLeft = 0f;

            if (_state.value == GuardAlertState.Patrolling)
                EnterState(GuardAlertState.Investigating, alarm);
        }

        /// <summary>The last words this guard took in, or null. Ends with the raid: a fresh garrison spawns each time.</summary>
        public string LastOverheard { get; private set; }

        /// <summary>How many spoken lines this guard has taken in.</summary>
        public int OverheardCount { get; private set; }

        /// <summary>Raised with the words whenever this guard takes in speech. The hook for later dialogue.</summary>
        public event Action<string> Overheard;

        /// <summary>
        /// <see cref="IEavesdropper"/>: runs after <see cref="OnNoiseHeard"/>, so a shout that woke this
        /// guard counts. An asleep or stunned guard hears the noise but not the words.
        /// </summary>
        public bool Overhear(NoiseEvent speech)
        {
            if (IsIncapacitated || string.IsNullOrWhiteSpace(speech.Transcript))
                return false;

            LastOverheard = speech.Transcript;
            OverheardCount++;
            Overheard?.Invoke(speech.Transcript);
            return true;
        }

        /// <summary>Noise at or above this strength wakes a sleeping guard.</summary>
        public const float NoiseWakeThreshold = 0.5f;

        // -----------------------------------------------------------------------------------------
        // Seeing
        // -----------------------------------------------------------------------------------------

        /// <summary>How long after a raid starts a calm garrison cannot see the players.</summary>
        public const float ArrivalGraceSeconds = 20f;

        private static float s_arrivalGraceEndsAt;

        /// <summary>
        /// Starts the arrival grace: until it runs out, and while the alarm is still calm, no guard
        /// sees a player. Guards still hear, so noise still draws them. Without it a patrol passing
        /// within sight of the gate killed players still reading the HUD (seen in co-op testing,
        /// 2026-09-23, with the garrison posted and patrolling two cells clear of the gate).
        /// </summary>
        public static void BeginArrivalGrace() => s_arrivalGraceEndsAt = Time.time + ArrivalGraceSeconds;

        /// <summary>Ends the arrival grace now. For tests: the grace is process-wide, so a test that
        /// started a raid would otherwise blind the guards of the next one.</summary>
        public static void EndArrivalGrace() => s_arrivalGraceEndsAt = 0f;

        /// <summary>The nearest intruder this guard can actually see, or null.</summary>
        public Transform FindVisibleIntruder(AlarmState alarm)
        {
            if (IsIncapacitated)
                return null;
            if (alarm == AlarmState.Calm && Time.time < s_arrivalGraceEndsAt)
                return null;

            Vector3 eye = transform.position + Vector3.up * _eyeHeight;
            float range = GuardBrain.SightRange(_sightRange, alarm);

            Transform best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < Intruders.Count; i++)
            {
                Transform intruder = Intruders[i];
                if (intruder == null)
                    continue;

                Vector3 target = intruder.position + Vector3.up * 1.0f;
                bool clear = !Physics.Linecast(eye, target, _geometryLayers);

                if (!GuardBrain.CanSee(eye, transform.forward, target, range, _fieldOfView, clear))
                    continue;

                float distance = Vector3.Distance(eye, target);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = intruder;
                }
            }

            return best;
        }

        // -----------------------------------------------------------------------------------------
        // Acting
        // -----------------------------------------------------------------------------------------

        private void Act(GuardAlertState state, Transform seen, AlarmState alarm, float deltaTime)
        {
            float speed = GuardBrain.MoveSpeed(_patrolSpeed, _chaseSpeed, state, alarm);
            _moveSpeed = speed;
            if (_agent != null && _agent.isOnNavMesh)
                _agent.speed = speed;

            switch (state)
            {
                case GuardAlertState.Incapacitated:
                    MoveTo(null);
                    return;

                case GuardAlertState.Chasing:
                    ChaseToward(seen != null ? seen.position : _lastKnownIntruderPosition, deltaTime);
                    if (seen != null)
                        TryAttack(seen);
                    break;

                case GuardAlertState.Searching:
                    Search();
                    break;

                case GuardAlertState.Investigating:
                    Investigate(deltaTime);
                    break;

                default:
                    Patrol();
                    break;
            }

            Steer(deltaTime, speed);
        }

        /// <summary>
        /// Closes on <paramref name="target"/> and halts <see cref="k_strikeStopFraction"/> of the strike
        /// range short of it, so the guard can strike but never walks its body into the player: the
        /// depenetration was shoving players through walls (#200). The agent's own stoppingDistance did
        /// not hold it (it braked into the target regardless), so the destination is the stopping point.
        /// </summary>
        private void ChaseToward(Vector3 target, float deltaTime)
        {
            float reach = _projectilePrefab != null ? GuardBrain.SightRange(_sightRange, CurrentAlarm) : _attackRange;
            float stopShort = reach * k_strikeStopFraction;

            Vector3 away = transform.position - target;
            away.y = 0f;
            if (away.magnitude <= stopShort)
            {
                // In position: stand and face the target instead of pressing on.
                MoveTo(null);
                Vector3 toward = -away;
                if (toward.sqrMagnitude > 0.0001f && deltaTime > 0f)
                {
                    Quaternion facing = Quaternion.LookRotation(toward, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, _turnSpeed * deltaTime);
                }
                return;
            }

            MoveTo(target + away.normalized * stopShort);
        }

        /// <summary>Goes to the last-known position, then sweeps reachable points around it instead of
        /// parking on it (#193).</summary>
        private void Search()
        {
            if (!_sweepGoal.HasValue)
                _sweepGoal = NextSweepGoal();

            MoveTo(_sweepGoal.Value);
            if (HasArrivedAt(_sweepGoal.Value) || TakeGaveUp())
                _sweepGoal = null;
        }

        private Vector3 NextSweepGoal()
        {
            if (_sweepIndex == 0)
            {
                _sweepIndex = 1;
                return _sweepCentre;
            }

            // A few tries: a ring point inside a wall or cut off from here is skipped, not walked at.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                Vector3? point = Reachable(_sweepCentre + GuardBrain.SweepOffset(_sweepIndex++));
                if (point.HasValue)
                    return point.Value;
            }
            return _sweepCentre;
        }

        /// <summary>Walks to the noise, looks around for a moment, then the state falls back to the
        /// route. Turning on the spot rather than standing there is the point (#193).</summary>
        private void Investigate(float deltaTime)
        {
            if (!_investigationTarget.HasValue)
                return;

            if (_lookTimeLeft > 0f)
            {
                MoveTo(null);
                transform.Rotate(Vector3.up, _turnSpeed * 0.5f * deltaTime, Space.World);
                _lookTimeLeft -= deltaTime;
                if (_lookTimeLeft <= 0f)
                    _investigationTarget = null;  // nothing here; back to the route
                return;
            }

            Vector3 target = _investigationTarget.Value;
            MoveTo(target);
            if (HasArrivedAt(target))
                _lookTimeLeft = GuardBrain.LookAroundSeconds;
            else if (TakeGaveUp())
                _investigationTarget = null;
        }

        private void Patrol()
        {
            int usable = 0;
            foreach (Transform waypoint in _patrolRoute)
                if (waypoint != null)
                    usable++;
            if (usable < 2)
            {
                Wander();
                return;
            }

            Transform point = _patrolRoute[_patrolIndex % _patrolRoute.Count];
            if (point == null)
            {
                _patrolIndex++;
                return;
            }

            MoveTo(point.position);
            // An unreachable point is skipped rather than walked at forever.
            if (HasArrivedAt(point.position) || TakeGaveUp())
                _patrolIndex = (_patrolIndex + 1) % _patrolRoute.Count;
        }

        /// <summary>A guard with no route to walk drifts between reachable points near where it was
        /// posted, instead of standing its post.</summary>
        private void Wander()
        {
            if (!_wanderGoal.HasValue)
            {
                Vector2 around = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(3f, 8f);
                Vector3 centre = _home ?? transform.position;
                _wanderGoal = Reachable(centre + new Vector3(around.x, 0f, around.y)) ?? centre;
            }

            MoveTo(_wanderGoal.Value);
            if (HasArrivedAt(_wanderGoal.Value) || TakeGaveUp())
                _wanderGoal = null;
        }

        // -----------------------------------------------------------------------------------------
        // Moving, snapping and the stuck watchdog (#193)
        // -----------------------------------------------------------------------------------------

        /// <summary>How far to look for the nearest NavMesh point when snapping a destination.</summary>
        private const float k_snapRadius = 6f;

        /// <summary>A destination that moves less than this is not sent to the agent again:
        /// SetDestination restarts path computation.</summary>
        private const float k_resendDistance = 0.75f;

        private const float k_strikeStopFraction = 0.8f;

        private float _moveSpeed;
        private NavMeshPath _scratchPath;
        private Vector3 _sentDestination;
        private bool _hasSentDestination;
        private Vector3 _watchGoal;
        private Vector3 _watchAnchor;
        private float _stuckTime;
        private int _stuckStage;
        private Vector3? _detour;
        private bool _gaveUp;

        private bool OnMesh => _agent != null && _agent.isOnNavMesh;

        /// <summary>The nearest NavMesh point to <paramref name="point"/>, or the point itself where
        /// there is no mesh to snap to (a player on a table, a noise inside a wall).</summary>
        private Vector3 SnapToMesh(Vector3 point)
        {
            if (OnMesh && NavMesh.SamplePosition(point, out NavMeshHit hit, k_snapRadius, NavMesh.AllAreas))
                return hit.position;
            return point;
        }

        /// <summary>
        /// A point this guard can actually walk to: snapped to the mesh and path-checked when there is
        /// one, the point unchanged when there is not, null when it is cut off.
        /// </summary>
        private Vector3? Reachable(Vector3 point)
        {
            if (!OnMesh)
                return point;
            if (!NavMesh.SamplePosition(point, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
                return null;
            _scratchPath ??= new NavMeshPath();
            if (!NavMesh.CalculatePath(transform.position, hit.position, NavMesh.AllAreas, _scratchPath)
                || _scratchPath.status != NavMeshPathStatus.PathComplete)
                return null;
            return hit.position;
        }

        /// <summary>True once, when the watchdog gave up on the destination it was watching.</summary>
        private bool TakeGaveUp()
        {
            bool gaveUp = _gaveUp;
            _gaveUp = false;
            return gaveUp;
        }

        private void MoveTo(Vector3? destination)
        {
            Destination = destination;

            if (!destination.HasValue)
            {
                _hasSentDestination = false;
                _stuckTime = 0f;
                _detour = null;
                if (OnMesh)
                    _agent.isStopped = true;
                _steerTarget = null;
                return;
            }

            Vector3 goal = destination.Value;
            WatchProgress(goal);
            Vector3 heading = SnapToMesh(_detour ?? goal);

            if (OnMesh)
            {
                _agent.isStopped = false;
                if (!_hasSentDestination || (heading - _sentDestination).sqrMagnitude > k_resendDistance * k_resendDistance
                    || (!_agent.hasPath && !_agent.pathPending))
                {
                    _agent.SetDestination(heading);
                    _sentDestination = heading;
                    _hasSentDestination = true;
                }
                return;
            }

            _steerTarget = heading;
        }

        /// <summary>
        /// Notices a guard that has a destination and is going nowhere: under
        /// <see cref="GuardBrain.StuckProgress"/> metres in <see cref="GuardBrain.StuckSeconds"/>.
        /// The first time it forces a fresh path; the second time it heads for a reachable point near
        /// the goal; and if there is none it gives the goal up, so the caller moves on to the next one.
        /// </summary>
        private void WatchProgress(Vector3 goal)
        {
            if (_moveSpeed <= 0.01f || _tickDelta <= 0f)
                return;   // a turret is meant not to move

            if (_seenThisTick)
            {
                // Closing on a target it can see, and standing to strike it, is not being stuck.
                _stuckTime = 0f;
                _stuckStage = 0;
                _detour = null;
                return;
            }

            if ((goal - _watchGoal).sqrMagnitude > 1.5f * 1.5f)
            {
                _watchGoal = goal;
                _watchAnchor = transform.position;
                _stuckTime = 0f;
                _stuckStage = 0;
                _detour = null;
            }

            Vector3 here = transform.position;
            if (HasArrivedAt(_detour ?? goal))
            {
                _watchAnchor = here;
                _stuckTime = 0f;
                if (_detour.HasValue)
                {
                    _detour = null;
                    _hasSentDestination = false;
                }
                return;
            }

            Vector3 moved = here - _watchAnchor;
            moved.y = 0f;
            if (!GuardBrain.IsStuck(moved.magnitude))
            {
                _watchAnchor = here;
                _stuckTime = 0f;
                _stuckStage = 0;
                return;
            }

            _stuckTime += _tickDelta;
            if (_stuckTime < GuardBrain.StuckSeconds)
                return;

            _stuckTime = 0f;
            _watchAnchor = here;
            _hasSentDestination = false;   // resend: a fresh path
            if (++_stuckStage == 1)
                return;

            _stuckStage = 0;
            Vector2 around = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(2f, 5f);
            Vector3? detour = OnMesh ? Reachable(goal + new Vector3(around.x, 0f, around.y)) : null;
            if (detour.HasValue)
                _detour = detour;
            else
                _gaveUp = true;
        }

        /// <summary>
        /// Where the guard is steering itself when there is no NavMeshAgent to do it. Applied in
        /// <see cref="Steer"/> rather than here so movement is tied to delta time.
        /// </summary>
        private Vector3? _steerTarget;

        /// <summary>
        /// Straight-line movement for a guard without a NavMeshAgent on a baked mesh.
        ///
        /// A procedurally generated castle has no baked NavMesh — it does not exist until the seed is
        /// known — so without this every guard in a generated raid can see and hear but never take a
        /// step, which reads as the AI being broken. It walks into walls where an agent would route
        /// around them; that is the honest trade for guards that move at all. Bake a mesh at runtime
        /// and they use it instead, automatically.
        /// </summary>
        private void Steer(float deltaTime, float speed)
        {
            if (!_steerTarget.HasValue || speed <= 0f || deltaTime <= 0f)
                return;

            Vector3 target = _steerTarget.Value;
            Vector3 flat = new Vector3(target.x - transform.position.x, 0f, target.z - transform.position.z);
            if (flat.sqrMagnitude <= _arrivalDistance * _arrivalDistance)
                return;

            Vector3 direction = flat.normalized;
            transform.position += direction * (speed * deltaTime);

            Quaternion facing = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, facing, _turnSpeed * deltaTime);
        }

        private bool HasArrivedAt(Vector3 position) =>
            Vector3.Distance(transform.position, SnapToMesh(position)) <= _arrivalDistance;

        private void EnterState(GuardAlertState next, AlarmState alarm)
        {
            GuardAlertState previous = _state.value;
            _state.value = next;
            _lookTimeLeft = 0f;
            if (next == GuardAlertState.Searching)
                ResetSweep();

            // Spotting an intruder is worth shouting about — once per chase, not once per frame. The
            // shout wakes guards in earshot; the director is told directly, walls or not (#139).
            bool firstSighting = false;
            if (next == GuardAlertState.Chasing && previous != GuardAlertState.Chasing)
            {
                if (!_hasShoutedThisChase)
                {
                    RaiseTheCry();
                    // Everyone in earshot is sent to where the intruder was seen, not to the shouter.
                    AlertGuardsNear(transform.position, _shoutRadius, _lastKnownIntruderPosition, this);
                    _hasShoutedThisChase = true;
                    firstSighting = true;
                }
            }
            else if (next == GuardAlertState.Patrolling)
            {
                _hasShoutedThisChase = false;
            }

            if (next == GuardAlertState.Chasing && previous != GuardAlertState.Chasing)
                _alarm?.Publish(new IntruderSpotted(this, NearestIntruder(), _lastKnownIntruderPosition, firstSighting));
            else if (previous == GuardAlertState.Chasing && next != GuardAlertState.Chasing)
                _alarm?.Publish(new IntruderLost(this, _lastKnownIntruderPosition));
            StateChanged?.Invoke(next);
        }

        /// <summary>
        /// Strikes or fires at <paramref name="target"/> when in range and off cooldown. A guard that
        /// could chase but never hurt anyone is what made the castle harmless.
        ///
        /// See docs/4-systems/raid.md, "Guards that can actually hurt you".
        /// </summary>
        private void TryAttack(Transform target)
        {
            if (IsIncapacitated || Time.time < _lastAttackTime + _attackCooldown)
                return;

            bool shoots = _projectilePrefab != null;
            float reach = shoots ? GuardBrain.SightRange(_sightRange, CurrentAlarm) : _attackRange;

            Vector3 origin = transform.position + Vector3.up * _eyeHeight;
            Vector3 toTarget = target.position + Vector3.up * 0.9f - origin;
            if (toTarget.magnitude > reach)
                return;

            _lastAttackTime = Time.time;
            SignalAttack(shoots ? GuardAttackKind.Projectile : GuardAttackKind.Melee);
            _alarm?.Publish(new GuardEngaged(this, target));

            if (shoots)
                FireAt(origin, toTarget.normalized);
            else if (target.TryGetComponent(out IHealth health))
                Damage.Apply(health, _attackDamage, gameObject, gameObject,
                    Damage.PointOn(target, origin), DamageKind.EnemyAttack);
        }

        /// <summary>
        /// Tells every peer this guard attacked. Server-side (the only side that runs TryAttack);
        /// clients hear it through the SyncVar and raise <see cref="Attacked"/> in
        /// <see cref="OnAttackSignalReplicated"/>.
        /// </summary>
        private void SignalAttack(GuardAttackKind kind)
        {
            _attackSignal.value = GuardAttackSignal.Next(_attackSignal.value, kind);
            Attacked?.Invoke(kind);
        }

        /// <summary>
        /// Spawns a projectile carrying this guard's damage, reusing the same
        /// <c>NetworkedProjectile</c> the player's spells fire so there is one projectile in the game
        /// rather than two.
        /// </summary>
        private void FireAt(Vector3 origin, Vector3 direction)
        {
            GameObject shot = Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));
            if (shot.TryGetComponent(out NetworkedProjectile projectile))
            {
                projectile.Damage = Mathf.RoundToInt(_attackDamage);
                projectile.Instigator = gameObject;
            }

            foreach (Collider own in GetComponentsInChildren<Collider>())
            {
                if (shot.TryGetComponent(out Collider shotCollider))
                    Physics.IgnoreCollision(shotCollider, own);
            }

            if (shot.TryGetComponent(out Rigidbody body))
                body.linearVelocity = direction * _projectileSpeed;
        }

        /// <summary>The castle-wide alert level, or Calm when this guard has no alarm to read.</summary>
        private AlarmState CurrentAlarm => _alarm != null ? _alarm.State : AlarmState.Calm;

        /// <summary>
        /// Forces this guard into <paramref name="next"/>. A dev and test seam in the same spirit as
        /// <see cref="Tick"/> being public: it lets a bench drop a guard straight into a chase rather
        /// than waiting for it to notice anyone. Not for gameplay — the AI owns its own transitions.
        /// </summary>
        public void SetAlertState(GuardAlertState next)
        {
            EnterState(next, _alarm != null ? _alarm.State : AlarmState.Calm);
        }

        /// <summary>
        /// The guard shouts. This goes through the ordinary acoustic path, so it reaches the alarm
        /// and every other guard in earshot — one guard spotting you is how a castle wakes up.
        /// </summary>
        public void RaiseTheCry()
        {
            NoiseBroadcaster.Broadcast(transform.position, _shoutRadius, _shoutStrength,
                NoiseType.VoiceCast, ~0, _geometryLayers);
        }

        // -----------------------------------------------------------------------------------------
        // Health
        // -----------------------------------------------------------------------------------------

        public void TakeDamage(float damage) => TakeDamage(damage, 0f);

        public void TakeDamage(float damage, float impactVelocity)
        {
            if (damage <= 0f || _health.value <= 0f)
                return;

            _health.value = Mathf.Max(0f, _health.value - damage);

            if (_health.value <= 0f)
            {
                _status?.ClearAll();
                EnterState(GuardAlertState.Incapacitated,
                    _alarm != null ? _alarm.State : AlarmState.Calm);
            }

            if (IsDead)
            {
                _alarm?.Publish(new GuardDied(this, transform.position));
                Destroy(this.gameObject);
            }
        }

        /// <summary>True once this guard is down for good, as opposed to merely asleep.</summary>
        public bool IsDead => _health.value <= 0f;

        // -----------------------------------------------------------------------------------------
        // Test seams
        // -----------------------------------------------------------------------------------------

        /// <summary>Wires the guard from code, for tests and tooling-built scenes.</summary>
        public void Configure(EnemyDirector alarm, List<Transform> patrolRoute = null)
        {
            // Null keeps the alarm Awake found: the spawner passes null, and overwriting it left
            // every spawned guard deaf to the alarm and unable to report a sighting (#163).
            if (alarm != null)
                WatchAlarm(alarm);
            if (patrolRoute != null)
                _patrolRoute = patrolRoute;
        }

        /// <summary>
        /// Scales this guard's movement speeds and damage per hit. Call once, on a freshly spawned
        /// guard: it multiplies the prefab's values, so a second call compounds.
        /// </summary>
        public void ScaleTuning(float speedScale, float damageScale)
        {
            _patrolSpeed *= speedScale;
            _chaseSpeed *= speedScale;
            _attackDamage *= damageScale;
        }

        /// <summary>Multiplies the guard's health, full and current, for a bigger lobby (#154).
        /// Applied once at spawn, on the server.</summary>
        public void ScaleHealth(float healthScale)
        {
            if (healthScale <= 0f)
                return;
            _maxHealth *= healthScale;
            _health.value = _maxHealth;
        }

        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float AttackDamage => _attackDamage;

        /// <summary>Where the guard is currently heading to investigate, if anywhere.</summary>
        public Vector3? InvestigationTarget => _investigationTarget;

        /// <summary>Where the guard last saw or heard an intruder: the centre its search sweeps around.</summary>
        public Vector3 LastKnownIntruderPosition => _lastKnownIntruderPosition;
    }
}

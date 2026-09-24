using System;
using System.Collections.Generic;
using Interfaces;
using PurrNet;
using RogueAi.Acoustics;
using RogueAi.Alarm;
using RogueAi.Status;
using UnityEngine;
using UnityEngine.AI;

namespace RogueAi.Guards
{
    /// <summary>
    /// A castle guard: the thing that makes noise matter.
    ///
    /// It hears (<see cref="INoiseListener"/>), it sees (a cone check with a line-of-sight raycast),
    /// and it can be shut down by the spells that target the living — Somnus, Tonitrus and Ignis all
    /// reach it through <see cref="StatusEffectReceiver"/>. Every decision it makes is delegated to
    /// <see cref="GuardBrain"/>, so its behaviour is asserted in tests rather than observed in play.
    ///
    /// Server-authoritative: only the server ticks the AI and moves the agent. Clients see the
    /// replicated transform and the replicated alert state.
    /// </summary>
    [RequireComponent(typeof(StatusEffectReceiver))]
    public class CastleGuard : NetworkBehaviour, INoiseListener, IHealth
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
        [SerializeField] private AlarmFSMManager _alarm;

        [Tooltip("Noise this guard makes when it spots an intruder and raises the cry.")]
        [Range(0f, 1f)] [SerializeField] private float _shoutStrength = 0.8f;
        [SerializeField] private float _shoutRadius = 20f;

        private readonly SyncVar<GuardAlertState> _state =
            new SyncVar<GuardAlertState>(GuardAlertState.Patrolling);

        [field: SerializeField] private SyncVar<float> _health { get; set; } = new SyncVar<float>(100f);

        private StatusEffectReceiver _status;
        private NavMeshAgent _agent;

        private Vector3? _investigationTarget;
        private Vector3 _lastKnownIntruderPosition;
        private float _timeSinceLastContact;
        private int _patrolIndex;
        private bool _hasShoutedThisChase;
        private float _lastAttackTime = float.NegativeInfinity;

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

        /// <summary>The intruders this guard is watching for. Registered by the player spawner.</summary>
        public static readonly List<Transform> Intruders = new List<Transform>();

        /// <summary>Registers a player as something guards will look for.</summary>
        public static void RegisterIntruder(Transform intruder)
        {
            if (intruder != null && !Intruders.Contains(intruder))
                Intruders.Add(intruder);
        }

        public static void UnregisterIntruder(Transform intruder) => Intruders.Remove(intruder);

        /// <summary>Forgets every intruder. Called between raids, and by test teardown.</summary>
        public static void ClearIntruders() => Intruders.Clear();

        private void Awake()
        {
            _status = GetComponent<StatusEffectReceiver>();
            _agent = GetComponent<NavMeshAgent>();

            // The agent moves this transform every frame; a dynamic rigidbody on the same object had
            // the physics step writing its own position back, so guards froze on about a third of
            // rendered frames and looked like they lagged and smeared (#104). Kinematic still
            // collides and still takes hits from thrown things.
            if (_agent != null && TryGetComponent(out Rigidbody body))
                body.isKinematic = true;
            _health.value = _maxHealth;

            if (_alarm == null)
                _alarm = FindObjectOfType<AlarmFSMManager>();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            // A client's guard is moved by the server's replicated transform. Its own agent would
            // fight that, and has no NavMesh to stand on until the client has built the castle.
            if (!isServer && _agent != null)
                _agent.enabled = false;
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

        /// <summary>True while Levo holds this guard up or it is still falling back down.</summary>
        public bool IsAirborne => _floating || _falling;

        /// <summary>
        /// Lifts the guard while it levitates and drops it when the spell ends. The guard's body is
        /// kinematic (the NavMeshAgent owns its position), so an impulse cannot lift it: the agent is
        /// suspended and the transform raised directly, then the body falls under real gravity and
        /// takes damage for the height. Returns true while the AI must not run.
        /// </summary>
        private bool UpdateLevitation(float deltaTime)
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
                        _agent.enabled = false;
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
                if (body != null)
                {
                    body.isKinematic = false;
                    body.useGravity = true;
                    body.freezeRotation = true;
                    body.linearVelocity = Vector3.zero;
                }
                else
                {
                    Land(body);
                    return false;
                }
                return true;
            }

            if (_falling)
            {
                bool settled = body == null || (Time.time - _fallStartedAt > 0.25f && Mathf.Abs(body.linearVelocity.y) < 0.05f);
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

            if (_agent != null && _health.value > 0f)
            {
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

            Transform seen = FindVisibleIntruder(alarm);
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

            Act(next, seen, alarm, deltaTime);
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

            // A louder noise overrides a quieter one already being walked toward.
            _investigationTarget = noise.Origin;

            if (_state.value == GuardAlertState.Patrolling)
                EnterState(GuardAlertState.Investigating, alarm);
        }

        /// <summary>Noise at or above this strength wakes a sleeping guard.</summary>
        public const float NoiseWakeThreshold = 0.5f;

        // -----------------------------------------------------------------------------------------
        // Seeing
        // -----------------------------------------------------------------------------------------

        /// <summary>The nearest intruder this guard can actually see, or null.</summary>
        public Transform FindVisibleIntruder(AlarmState alarm)
        {
            if (IsIncapacitated)
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
            if (_agent != null && _agent.isOnNavMesh)
                _agent.speed = speed;

            switch (state)
            {
                case GuardAlertState.Incapacitated:
                    MoveTo(null);
                    return;

                case GuardAlertState.Chasing:
                    MoveTo(seen != null ? seen.position : _lastKnownIntruderPosition);
                    if (seen != null)
                        TryAttack(seen);
                    break;

                case GuardAlertState.Searching:
                    MoveTo(_lastKnownIntruderPosition);
                    break;

                case GuardAlertState.Investigating:
                    if (_investigationTarget.HasValue)
                    {
                        MoveTo(_investigationTarget.Value);
                        if (HasArrivedAt(_investigationTarget.Value))
                            _investigationTarget = null;  // nothing here; back to the route
                    }
                    break;

                default:
                    Patrol();
                    break;
            }

            Steer(deltaTime, speed);
        }

        private void Patrol()
        {
            if (_patrolRoute.Count == 0)
            {
                MoveTo(null);
                return;
            }

            Transform point = _patrolRoute[_patrolIndex % _patrolRoute.Count];
            if (point == null)
            {
                _patrolIndex++;
                return;
            }

            MoveTo(point.position);
            if (HasArrivedAt(point.position))
                _patrolIndex = (_patrolIndex + 1) % _patrolRoute.Count;
        }

        private void MoveTo(Vector3? destination)
        {
            Destination = destination;

            if (_agent != null && _agent.isOnNavMesh)
            {
                if (destination.HasValue)
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(destination.Value);
                }
                else
                {
                    _agent.isStopped = true;
                }
                return;
            }

            _steerTarget = destination;
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
            Vector3.Distance(transform.position, position) <= _arrivalDistance;

        private void EnterState(GuardAlertState next, AlarmState alarm)
        {
            GuardAlertState previous = _state.value;
            _state.value = next;

            // Spotting an intruder is worth shouting about — once per chase, not once per frame.
            if (next == GuardAlertState.Chasing && previous != GuardAlertState.Chasing)
            {
                if (!_hasShoutedThisChase)
                {
                    RaiseTheCry();
                    _hasShoutedThisChase = true;
                }
            }
            else if (next == GuardAlertState.Patrolling)
            {
                _hasShoutedThisChase = false;
            }

            StateChanged?.Invoke(next);
        }

        /// <summary>
        /// Strikes or fires at <paramref name="target"/> when in range and off cooldown. A guard that
        /// could chase but never hurt anyone is what made the castle harmless.
        ///
        /// See docs/systems/raid.md, "Guards that can actually hurt you".
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

            if (shoots)
                FireAt(origin, toTarget.normalized);
            else if (target.TryGetComponent(out IHealth health))
                Damage.Apply(health, _attackDamage, gameObject, gameObject,
                    Damage.PointOn(target, origin), DamageKind.EnemyAttack);
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
                Destroy(this.gameObject);
            }
        }

        /// <summary>True once this guard is down for good, as opposed to merely asleep.</summary>
        public bool IsDead => _health.value <= 0f;

        // -----------------------------------------------------------------------------------------
        // Test seams
        // -----------------------------------------------------------------------------------------

        /// <summary>Wires the guard from code, for tests and tooling-built scenes.</summary>
        public void Configure(AlarmFSMManager alarm, List<Transform> patrolRoute = null)
        {
            _alarm = alarm;
            if (patrolRoute != null)
                _patrolRoute = patrolRoute;
        }

        /// <summary>Where the guard is currently heading to investigate, if anywhere.</summary>
        public Vector3? InvestigationTarget => _investigationTarget;
    }
}

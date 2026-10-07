using System;
using Interfaces;
using PurrNet;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Status;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The fresh guard (#206): a thin networked host. It owns the replicated values (state, health and
    /// attack signal are SyncVars, which must be fields of a NetworkBehaviour), the state machine, and
    /// the parts the states use: sight, hearing, navigator, health, shove and the attack signal. It makes
    /// no decisions itself; each state decides what the guard does and which state comes next. It moves
    /// only through the director's navigation (see <see cref="GuardNavigator"/>).
    ///
    /// Server-authoritative: only the server runs the AI. Clients see the replicated transform and the
    /// replicated state. The legacy <c>CastleGuard</c> is no longer on any prefab (#214) and stays
    /// compiling, unused, until the owner approves removing it; inventory in
    /// docs/plans/guard-core-inventory.md.
    /// </summary>
    [RequireComponent(typeof(StatusEffectReceiver), typeof(GuardDeathPlayback))]
    public class Guard : NetworkBehaviour, INoiseListener, IHealth, IShovable
    {
        [SerializeField] private GuardTuning _tuning = new GuardTuning();

        private readonly SyncVar<GuardAlertState> _state = new SyncVar<GuardAlertState>(GuardAlertState.Patrolling);
        private readonly SyncVar<float> _health = new SyncVar<float>(100f);
        private readonly SyncVar<int> _attackSignal = new SyncVar<int>(GuardAttackSignal.None);

        private readonly StateMachine<Guard> _machine = new StateMachine<Guard>();
        private EnemyDirector _configuredDirector;
        private bool _built;

        /// <summary>Raised on this peer when the guard's replicated state changes.</summary>
        public event Action<GuardAlertState> StateChanged;

        public GuardTuning Tuning => _tuning;

        /// <summary>The guard's own random numbers, seeded from where it was posted so the server picks the
        /// same patrol every time. Tests replace the seed with <see cref="Reseed"/>.</summary>
        public System.Random Random { get; private set; }

        public StatusEffectReceiver Status { get; private set; }
        public GuardHealth Health { get; private set; }
        public GuardSight Sight { get; private set; }
        public GuardHearing Hearing { get; private set; }
        public GuardNavigator Navigator { get; private set; }
        public GuardDirectorLink Link { get; private set; }
        public GuardAttackSignaller AttackSignal { get; private set; }

        /// <summary>The ranged shot (#209). Chase and Combat both fire through it.</summary>
        public GuardRangedAttack RangedAttack { get; private set; }

        /// <summary>The melee strike (#210). Combat strikes through it when the guard has a turn.</summary>
        public GuardMeleeAttack MeleeAttack { get; private set; }

        /// <summary>Whether a player can be got at on foot (#237). Chase and Combat ask it of melee guards.</summary>
        public GuardReachability Reach { get; private set; }

        /// <summary>This guard's asking for, and holding of, an attack turn (#210).</summary>
        public GuardAttackTurn AttackTurn { get; private set; }

        /// <summary>What is worth a look (noise, sighting, hue and cry). Patrol and Investigate read it (#208).</summary>
        public GuardLeads Leads { get; private set; }
        public GuardStateSet States { get; private set; }

        /// <summary>The cry for help on a first sighting (#259).</summary>
        public GuardCry Cry { get; private set; }

        /// <summary>Levo's lift and the fall after it (the guard has no physics body for the spell to push).</summary>
        public GuardLift Lift { get; private set; }
        private GuardShove _shove;

        /// <summary>Where the guard was posted. Patrol wanders around it.</summary>
        public Vector3 Home { get; private set; }

        /// <summary>The replicated state clients, audio and the HUD read.</summary>
        public GuardAlertState State => _state.value;

        /// <summary>The state running right now, on the server.</summary>
        public State<Guard> CurrentState => _machine.Current;

        public bool IsDead => Health.IsDead;

        /// <summary>The server (or anything not networked, such as a test) runs the AI.</summary>
        public bool IsAuthority => !isSpawned || isServer;

        // IHealth
        public float CurrentHealth => Health.Current;
        public float MaxHealth => Health.Max;
        public void TakeDamage(float damage) => Health.TakeDamage(damage);
        public void TakeDamage(float damage, float impactVelocity) => Health.TakeDamage(damage);

        // IShovable
        void IShovable.Shove(Vector3 metres) => _shove.Start(metres);

        private void Awake() => Build();

        // Built on demand so a test (or the spawner) that touches the guard before Awake still gets parts.
        private void Build()
        {
            if (_built)
                return;
            _built = true;

            Status = GetComponent<StatusEffectReceiver>();
            Home = transform.position;
            Random = new System.Random(SeedFrom(Home));
            Health = new GuardHealth(_health, _tuning.MaxHealth);
            Sight = new GuardSight(transform, _tuning, GetInstanceID());
            Hearing = new GuardHearing(Status);
            Navigator = new GuardNavigator(this, _tuning);
            Link = new GuardDirectorLink(this);
            AttackSignal = new GuardAttackSignaller(_attackSignal);
            RangedAttack = new GuardRangedAttack(this);
            MeleeAttack = new GuardMeleeAttack(this);
            AttackTurn = new GuardAttackTurn(this);
            Reach = new GuardReachability(this);
            _ = new GuardHelpResponse(this);
            Leads = new GuardLeads(Link, Hearing, () => Random);
            Cry = new GuardCry(this);
            Hearing.NoiseHeard += (origin, strength) => Link.Director?.Publish(new NoiseReported(this, origin, strength));
            _shove = new GuardShove(transform, _tuning);
            Lift = new GuardLift(transform, Status);
            States = new GuardStateSet(this);

            Health.Died += OnDied;
            Status.StatusChanged += OnStatusChanged;
            _machine.StateChanged += OnMachineStateChanged;
            _machine.ChangeState(States.Patrol);
        }

        private void OnEnable()
        {
            Build();
            AttachTo(_configuredDirector != null ? _configuredDirector : FindDirector());
        }

        private void OnDisable() => AttachTo(null);

        protected override void OnDestroy()
        {
            if (_built)
            {
                Health.Died -= OnDied;
                Status.StatusChanged -= OnStatusChanged;
            }
            base.OnDestroy();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (isServer)
                return;
            // A client replays what the server decided.
            AttackSignal.StartReplaying();
            _state.onChanged += OnReplicatedState;
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            AttackSignal.StopReplaying();
            _state.onChanged -= OnReplicatedState;
        }

        private static EnemyDirector FindDirector()
        {
            return EnemyDirector.Current != null ? EnemyDirector.Current : FindFirstObjectByType<EnemyDirector>();
        }

        private void AttachTo(EnemyDirector director)
        {
            Link.Attach(director);
            Navigator.Attach(director);
        }

        /// <summary>Wires the guard to a director from code, for tests and tooling-built scenes. Null keeps the
        /// director the guard found when it woke: a spawned guard must not lose the alarm it reports to.</summary>
        public void Configure(EnemyDirector director)
        {
            if (director == null)
                return;

            _configuredDirector = director;
            if (isActiveAndEnabled)
                AttachTo(director);
        }

        private void Update()
        {
            // Clients render what the server decided; only the server runs the AI.
            if (IsAuthority)
                Tick(Time.deltaTime);
        }

        /// <summary>Advances the guard one step: look, run the state, apply any shove. Public and
        /// network-free so a test can step a guard without a scene running.</summary>
        public void Tick(float deltaTime)
        {
            Sight.Update(EnemyDirector.IntrudersOf(Link.Director), Link.Alarm, Status.IsIncapacitated, deltaTime);
            _machine.Tick(deltaTime);
            _shove.Step(deltaTime);
            Lift.Step(deltaTime);
        }

        /// <summary>A noise reached the guard. The server decides; a client's copy ignores it.</summary>
        public void OnNoiseHeard(NoiseEvent noise)
        {
            // A guard does not hear its own cry; the broadcast reaches the crier too.
            if (IsAuthority && !Cry.IsCrying)
                Hearing.Hear(noise, Link.Alarm);
        }

        /// <summary>Starts the guard's random numbers over from <paramref name="seed"/>.</summary>
        public void Reseed(int seed) => Random = new System.Random(seed);

        // Centimetre-rounded so the same post gives the same seed on every run and every peer.
        private static int SeedFrom(Vector3 post)
        {
            return Mathf.RoundToInt(post.x * 100f) * 73856093 ^ Mathf.RoundToInt(post.z * 100f) * 19349663;
        }

        /// <summary>Switches state from outside. A seam for tests, and for later states that hand over to each other.</summary>
        public void ChangeState(GuardState next) => _machine.ChangeState(next);

        /// <summary>Scales speeds and damage for the lobby size. Call once on a fresh guard: it multiplies.</summary>
        public void ScaleTuning(float speedScale, float damageScale)
        {
            _tuning.PatrolSpeed *= speedScale;
            _tuning.ChaseSpeed *= speedScale;
            _tuning.AttackDamage *= damageScale;
        }

        /// <summary>Multiplies health for a bigger lobby (#154). Call once, on the server, at spawn.</summary>
        public void ScaleHealth(float healthScale) => Health.Scale(healthScale);

        // A status effect that incapacitates or ignites is not a state's choice: it interrupts whichever state is running.
        private void OnStatusChanged(StatusEffectReceiver status)
        {
            if (IsAuthority && !IsDead && States.TryInterrupt(status, out GuardState held))
                _machine.ChangeState(held);
        }

        private void OnDied()
        {
            Status.ClearAll();
            Link.Director?.Publish(new GuardDied(this, transform.position));
            _machine.ChangeState(States.Dead);
        }

        private void OnMachineStateChanged(State<Guard> previous, State<Guard> next)
        {
            GuardAlertState alert = ((GuardState)next).AlertState;
            if (IsAuthority)
                _state.value = alert;
            StateChanged?.Invoke(alert);
        }

        private void OnReplicatedState(GuardAlertState alert) => StateChanged?.Invoke(alert);
    }
}

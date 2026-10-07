using System;
using Code.Scripts.EventSystems;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The server-side enemy mediator (#205), kept thin: a NetworkBehaviour that owns the parts and ticks
    /// them (<see cref="EnemyRegistry"/>, <see cref="EnemyDirectorBus"/>, <see cref="DirectorAlarm"/>,
    /// <see cref="HueAndCry"/>, <see cref="GuardNavigationService"/>, <see cref="AttackTurnMediator"/>).
    /// Nothing here moves a guard: a request is only relayed, and each guard's state machine decides.
    /// It lives in the Alarm assembly, below Guards, so guards are <see cref="Component"/>s and events plain data.
    /// PurrNet 1.15 has no SyncVar hooks, and a <see cref="SyncVar{T}"/> must be a field of this class, so the
    /// alarm is handed them; a state change reaches every peer through <see cref="BroadcastAlarmState"/>.
    /// </summary>
    public class EnemyDirector : NetworkBehaviour
    {
        [Header("Tuning")]
        [Tooltip("Alarm points added per unit of noise strength.")]
        [SerializeField] private float _noiseWeight = 15f;

        [Tooltip("Seconds of silence before the alarm begins to decay.")]
        [SerializeField] private float _decayDelay = 3.0f;

        [Tooltip("Alarm points shed per second while decaying.")]
        [SerializeField] private float _decayRate = 5.0f;

        [Header("Guards")]
        [Tooltip("Alarm points each time a guard spots an intruder and gives chase.")]
        [SerializeField] private float _sightingPoints = 20f;

        [Tooltip("Alarm points each time a guard's attack is launched at a player.")]
        [SerializeField] private float _attackPoints = 6f;

        [Tooltip("Guards chasing at once that force the castle to at least Roused.")]
        [SerializeField] private int _rousedChasers = 3;

        [Tooltip("Guards chasing at once that force Hue and Cry.")]
        [SerializeField] private int _hueAndCryChasers = 5;

        [Tooltip("Distinct guards that must have seen an intruder this raid before the alarm can be Roused (the lockdown).")]
        [SerializeField] private int _rousedWitnesses = 3;

        [Tooltip("Distinct guards that must have seen an intruder this raid before the alarm can be Hue and Cry.")]
        [SerializeField] private int _hueAndCryWitnesses = 5;

        [Header("Hue and cry")]
        [Tooltip("Seconds between repeats of the hue and cry, at the players' current positions, while the alarm stays at Hue and Cry.")]
        [SerializeField] private float _hueAndCryRepeatSeconds = 3f;

        [Header("Attack turns")]
        [SerializeField] private AttackTurnTuning _attackTurnTuning = new AttackTurnTuning();

        [Header("Guard navigation")]
        [SerializeField] private GuardNavigationTuning _navigationTuning = new GuardNavigationTuning();

        private readonly SyncVar<float> _alarmLevel = new SyncVar<float>(0f);
        private readonly SyncVar<AlarmState> _alarmState = new SyncVar<AlarmState>(AlarmState.Calm);
        private readonly EnemyRegistry _registry = new EnemyRegistry();

        // The parts below are made on first use, after the Inspector values load, so EditMode tests need no Awake.
        private DirectorAlarm _alarm;
        private EnemyDirectorBus _bus;
        private HueAndCry _hueAndCry;
        private AttackTurnMediator _attackTurns;
        private GuardNavigationService _navigation;

        /// <summary>The director in play, set while one is enabled. Guards and intruder tags register with it; the most recently enabled wins.</summary>
        public static EnemyDirector Current { get; private set; }

        /// <summary>Raised on every peer whenever the alarm state changes. UI and enemy AI subscribe.</summary>
        public event Action<AlarmState> AlarmStateChanged;

        public float AlarmLevel => Alarm.Level;
        public AlarmState State => Alarm.State;
        public bool IsLocked => Alarm.IsLocked;

        /// <summary>True during the calm grace after a raid starts, when nothing raises the alarm.</summary>
        public bool InGrace => Alarm.InGrace;

        /// <summary>How many guards are chasing an intruder right now.</summary>
        public int ChasingGuards => Alarm.ChasingGuards;

        internal bool IsAuthority => !isSpawned || isServer;

        internal DirectorAlarm Alarm => _alarm ??= new DirectorAlarm(_alarmLevel, _alarmState, new AlarmTuning(
            _noiseWeight, _decayDelay, _decayRate, _sightingPoints, _attackPoints, _rousedChasers, _hueAndCryChasers,
            _rousedWitnesses, _hueAndCryWitnesses),
            OnAlarmStateChanged);

        private EnemyDirectorBus Bus => _bus ??= new EnemyDirectorBus(this);

        /// <summary>Who may attack which player right now (#210).</summary>
        public AttackTurnMediator AttackTurns => _attackTurns ??= new AttackTurnMediator(this, _attackTurnTuning);

        /// <summary>Moves guards on request (#222). Give it a map with <see cref="GuardNavigationService.SetMap"/> before sending requests.</summary>
        public GuardNavigationService Navigation => _navigation ??= new GuardNavigationService(this, _navigationTuning);

        // Registries ---------------------------------------------------------------------------------

        /// <summary>Guards alive and enabled, as <see cref="Component"/>: the director sits below the Guards assembly.</summary>
        public IReadOnlyList<Component> Guards => _registry.Guards;

        /// <summary>The players guards look for. Registered by <c>IntruderTag</c>.</summary>
        public IReadOnlyList<Transform> Intruders => _registry.Intruders;

        public static IReadOnlyList<Component> GuardsOf(EnemyDirector director)
            => director != null ? director.Guards : EnemyRegistry.NoGuards;

        public static IReadOnlyList<Transform> IntrudersOf(EnemyDirector director)
            => director != null ? director.Intruders : EnemyRegistry.NoIntruders;

        public void RegisterGuard(Component guard) => _registry.AddGuard(guard);

        /// <summary>Removes a guard from the registry and takes back any attack turn it held.</summary>
        public void UnregisterGuard(Component guard)
        {
            _registry.RemoveGuard(guard);
            ReleaseAttackTurnOf(guard);
        }

        public void RegisterIntruder(Transform intruder) => _registry.AddIntruder(intruder);

        public void UnregisterIntruder(Transform intruder) => _registry.RemoveIntruder(intruder);

        public void ClearIntruders() => _registry.ClearIntruders();

        public bool IsIntruder(Transform intruder) => _registry.IsIntruder(intruder);

        // The guards' reports and the director's requests travel on EventManager; the bus below is the
        // director's ear for the ones it must act on (score the alarm, hand out attack turns).

        // Alarm ----------------------------------------------------------------------------------------

        public void ReportSighting(int guardId) => Alarm.ReportSighting(guardId);

        public void ReportAttack() => Alarm.ReportAttack();

        public void ReportChase(int guardId, bool chasing) => Alarm.ReportChase(guardId, chasing);

        public void ResetForNewRaid(float graceSeconds) => Alarm.ResetForNewRaid(graceSeconds);

        public void TickDecay(float deltaTime) => Alarm.TickDecay(deltaTime);

        /// <summary>Test/setup helper: force the alarm level and immediately re-evaluate state.</summary>
        public void SetAlarmLevel(float level, int witnesses = 0) => Alarm.SetLevel(level, witnesses);

        public void UpdateState() => Alarm.UpdateState();

        // Lifecycle --------------------------------------------------------------------------------------

        private void Awake() => Current = this;

        private void OnEnable()
        {
            Current = this;
            Bus.Listen();
            _navigation?.Listen();
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
            Bus.StopListening();
            _navigation?.StopListening();
        }

        protected override void OnSpawned()
        {
            base.OnSpawned();
            Alarm.NoteNoiseNow();
        }

        private float _publishedLevel = -1f;

        private void Update()
        {
            // Every peer, including clients that only receive the replicated value, tells listeners when it moves.
            if (Alarm.Level != _publishedLevel)
            {
                _publishedLevel = Alarm.Level;
                EventManager.Instance?.Publish(new AlarmLevelChanged(_publishedLevel));
            }

            // Only the server integrates decay; clients receive state via replication.
            if (isSpawned && !isServer)
                return;
            Alarm.TickDecay(Time.deltaTime);
            TickHueAndCry(Time.deltaTime);
            _navigation?.Tick(Time.deltaTime);
            _attackTurns?.Tick(Time.deltaTime);
        }

        /// <summary>Repeats the hue and cry on its interval while the alarm is at Hue and Cry; does nothing otherwise or on a client.</summary>
        public void TickHueAndCry(float deltaTime)
        {
            if (IsAuthority && State == AlarmState.HueAndCry)
                HueAndCryRaiser.Repeat(deltaTime);
        }

        private HueAndCry HueAndCryRaiser => _hueAndCry ??= new HueAndCry(_registry, PublishInvestigate, _hueAndCryRepeatSeconds);

        private static void PublishInvestigate(InvestigateRequest request) => EventManager.Instance?.Publish(request);

        internal void ReleaseAttackTurnOf(Component guard) => _attackTurns?.Release(guard);

        // The hue and cry is a request, not an order: guards decide what to do with it.
        private void OnAlarmStateChanged(AlarmState newState)
        {
            if (newState == AlarmState.HueAndCry && IsAuthority)
                HueAndCryRaiser.Raise();

            if (isSpawned && isServer)
                BroadcastAlarmState(newState);
            else
                RaiseStateChanged(newState); // single-player / EditMode tests
        }

        /// <summary>Fans the new state out to every observer and raises the local event on each.</summary>
        [ObserversRpc(bufferLast: true)]
        private void BroadcastAlarmState(AlarmState newState) => RaiseStateChanged(newState);

        private void RaiseStateChanged(AlarmState newState)
        {
            AlarmStateChanged?.Invoke(newState);
            EventManager.Instance?.Publish(new AlarmChanged(newState));
        }
    }
}

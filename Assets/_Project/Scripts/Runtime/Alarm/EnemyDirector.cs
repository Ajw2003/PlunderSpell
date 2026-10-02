using System;
using System.Collections.Generic;
using PurrNet;
using Plunderspell.Acoustics;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The server-side enemy mediator (#205). It owns the guard and intruder registries, a small typed
    /// event bus that guards and listeners share, and the alarm (formerly AlarmFSMManager, folded in by
    /// the owner's decision). Guards raise events; the director scores the alarm and answers with events
    /// such as <see cref="InvestigateRequest"/>. Nothing here moves a guard: a request is only relayed,
    /// and each guard's own state machine decides what to do with it.
    ///
    /// It lives in the Alarm assembly because Guards, Audio, Castle, Raid and UI already reference it, so
    /// none gains a dependency. Guards are held as <see cref="Component"/> and events carry plain data,
    /// so it needs no reference to Guards.
    ///
    /// The alarm is a server-authoritative four-state machine. It listens for noise (as an
    /// <see cref="INoiseListener"/>), accumulates an alarm level 0–100 weighted by noise strength, and
    /// drives the shared <see cref="AlarmState"/> that UI and enemy AI subscribe to.
    ///
    /// Decay rules: while <see cref="AlarmState.Calm"/>/<see cref="AlarmState.Stirred"/> the level bleeds
    /// off at 5/sec once no noise has arrived for 3 seconds. Reaching <see cref="AlarmState.Roused"/> or
    /// <see cref="AlarmState.HueAndCry"/> latches <see cref="IsLocked"/> — from then on the alarm never
    /// decays and the state can only escalate, for the rest of the raid.
    /// <see cref="ResetForNewRaid"/> clears the latch and opens a grace in which nothing raises it.
    ///
    /// Guards do not only make noise: seeing an intruder (<see cref="ReportSighting"/>), landing a
    /// blow (<see cref="ReportAttack"/>) and how many of them are chasing at once
    /// (<see cref="ReportChase"/>) go straight to the alarm, un-muffled by walls. Several guards
    /// fighting you is the castle up in arms whether or not the shout carried (#139).
    ///
    /// PurrNet 1.15 note: there is no Mirror-style <c>[SyncVar(hook=...)]</c> — replicated state uses
    /// field-based <see cref="SyncVar{T}"/> modules. The state change fans out to clients via the
    /// <c>[ObserversRpc]</c> <see cref="BroadcastAlarmState"/>, which raises the local
    /// <see cref="AlarmStateChanged"/> C# event on every peer. Pure logic
    /// (<see cref="ApplyNoise"/>, <see cref="UpdateState"/>, <see cref="TickDecay"/>) is network-free
    /// for EditMode testing.
    /// </summary>
    public class EnemyDirector : NetworkBehaviour, INoiseListener
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
        [SerializeField] private int _rousedChasers = 2;

        [Tooltip("Guards chasing at once that force Hue and Cry.")]
        [SerializeField] private int _hueAndCryChasers = 3;

        // Replicated state (PurrNet field-based SyncVars; inline-initialised so never null).
        private readonly SyncVar<float> _alarmLevel = new SyncVar<float>(0f);
        private readonly SyncVar<AlarmState> _alarmState = new SyncVar<AlarmState>(AlarmState.Calm);

        private float _lastNoiseTime;   // server-only timestamp of the most recent noise
        private bool _locked;           // true once Roused/HueAndCry reached — stops all decay
        private float _graceEndsAt;     // server-only: nothing raises the alarm before this time
        private readonly HashSet<int> _chasers = new HashSet<int>(); // guards chasing right now

        // Thresholds.
        private const float StirredThreshold = 20f;
        private const float RousedThreshold = 50f;
        private const float HueAndCryThreshold = 80f;

        /// <summary>Raised on every peer whenever the alarm state changes. UI and enemy AI subscribe.</summary>
        public event Action<AlarmState> AlarmStateChanged;

        public float AlarmLevel => _alarmLevel.value;
        public AlarmState State => _alarmState.value;
        public bool IsLocked => _locked;

        /// <summary>True during the calm grace after a raid starts, when nothing raises the alarm.</summary>
        public bool InGrace => Time.time < _graceEndsAt;

        /// <summary>How many guards are chasing an intruder right now.</summary>
        public int ChasingGuards => _chasers.Count;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            _lastNoiseTime = Time.time;
        }

        // ---------------------------------------------------------------------------------------
        // Registries
        // ---------------------------------------------------------------------------------------

        private readonly List<Component> _guards = new List<Component>();
        private readonly List<Transform> _intruders = new List<Transform>();

        /// <summary>The director in play, set while one is enabled. Guards and intruder tags register
        /// with it; the most recently enabled wins.</summary>
        public static EnemyDirector Current { get; private set; }

        private static readonly List<Component> s_noGuards = new List<Component>();
        private static readonly List<Transform> s_noIntruders = new List<Transform>();

        /// <summary>Guards alive and enabled. Listed as <see cref="Component"/>: the director sits below
        /// the Guards assembly, so callers cast to their guard type.</summary>
        public IReadOnlyList<Component> Guards => _guards;

        /// <summary>The players guards look for. Registered by <c>IntruderTag</c>.</summary>
        public IReadOnlyList<Transform> Intruders => _intruders;

        /// <summary>Guards of <paramref name="director"/>, or an empty list when there is none.</summary>
        public static IReadOnlyList<Component> GuardsOf(EnemyDirector director)
            => director != null ? director._guards : s_noGuards;

        /// <summary>Intruders of <paramref name="director"/>, or an empty list when there is none.</summary>
        public static IReadOnlyList<Transform> IntrudersOf(EnemyDirector director)
            => director != null ? director._intruders : s_noIntruders;

        /// <summary>Adds a guard to the registry.</summary>
        public void RegisterGuard(Component guard)
        {
            if (guard != null && !_guards.Contains(guard))
                _guards.Add(guard);
        }

        /// <summary>Removes a guard from the registry.</summary>
        public void UnregisterGuard(Component guard) => _guards.Remove(guard);

        /// <summary>Registers a player as something guards will look for.</summary>
        public void RegisterIntruder(Transform intruder)
        {
            if (intruder != null && !_intruders.Contains(intruder))
                _intruders.Add(intruder);
        }

        /// <summary>Stops guards looking for <paramref name="intruder"/>.</summary>
        public void UnregisterIntruder(Transform intruder) => _intruders.Remove(intruder);

        /// <summary>Forgets every intruder.</summary>
        public void ClearIntruders() => _intruders.Clear();

        /// <summary>True when <paramref name="intruder"/> is registered.</summary>
        public bool IsIntruder(Transform intruder) => _intruders.Contains(intruder);

        private void Awake() => Current = this;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        // ---------------------------------------------------------------------------------------
        // Event bus. Plain C# events carrying readonly structs: raising allocates nothing.
        // ---------------------------------------------------------------------------------------

        /// <summary>A noise reached the director.</summary>
        public event Action<NoiseReported> OnNoiseReported;
        /// <summary>A guard spotted an intruder.</summary>
        public event Action<IntruderSpotted> OnIntruderSpotted;
        /// <summary>A guard gave up a chase.</summary>
        public event Action<IntruderLost> OnIntruderLost;
        /// <summary>A guard attacked.</summary>
        public event Action<GuardEngaged> OnGuardEngaged;
        /// <summary>The alarm state changed (on every peer).</summary>
        public event Action<AlarmChanged> OnAlarmChanged;
        /// <summary>A guard died.</summary>
        public event Action<GuardDied> OnGuardDied;
        /// <summary>Guards are asked to investigate a position. Relayed only; each guard decides.</summary>
        public event Action<InvestigateRequest> OnInvestigateRequest;
        /// <summary>A guard is asked to walk somewhere. The navigation service answers it (#222).</summary>
        public event Action<MoveRequest> OnMoveRequest;
        /// <summary>The navigation service planned a route and the guard is walking it.</summary>
        public event Action<PathReady> OnPathReady;
        /// <summary>A guard reached the spot it was sent to.</summary>
        public event Action<Arrived> OnArrived;
        /// <summary>A guard could not get to the spot it was sent to.</summary>
        public event Action<Blocked> OnBlocked;

        private bool IsAuthority => !isSpawned || isServer;

        /// <summary>A guard saw an intruder: scores the sighting and counts the chaser.</summary>
        public void Publish(IntruderSpotted e)
        {
            OnIntruderSpotted?.Invoke(e);
            if (!IsAuthority)
                return;
            if (e.FirstSighting)
                ReportSighting();
            ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, true);
        }

        /// <summary>A guard stopped chasing: stops counting it. Stopping never lowers the alarm.</summary>
        public void Publish(IntruderLost e)
        {
            OnIntruderLost?.Invoke(e);
            if (IsAuthority)
                ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        /// <summary>A guard attacked: scores the attack.</summary>
        public void Publish(GuardEngaged e)
        {
            OnGuardEngaged?.Invoke(e);
            if (IsAuthority)
                ReportAttack();
        }

        /// <summary>A guard died: stops counting it as a chaser.</summary>
        public void Publish(GuardDied e)
        {
            OnGuardDied?.Invoke(e);
            if (IsAuthority)
                ReportChase(e.Guard != null ? e.Guard.GetInstanceID() : 0, false);
        }

        /// <summary>Raises a noise event and scores it.</summary>
        public void Publish(NoiseReported e)
        {
            OnNoiseReported?.Invoke(e);
            if (IsAuthority)
                ApplyNoise(e.Strength);
        }

        /// <summary>Asks guards to investigate. The director never moves a guard itself.</summary>
        public void Publish(InvestigateRequest e) => OnInvestigateRequest?.Invoke(e);

        /// <summary>Asks the navigation service to walk a guard. Touching <see cref="Navigation"/> first
        /// makes sure the service exists to hear it.</summary>
        public void Publish(MoveRequest e)
        {
            _ = Navigation;
            OnMoveRequest?.Invoke(e);
        }

        public void Publish(PathReady e) => OnPathReady?.Invoke(e);

        public void Publish(Arrived e) => OnArrived?.Invoke(e);

        public void Publish(Blocked e) => OnBlocked?.Invoke(e);

        /// <summary>The hue and cry: one investigate request at each player, so each guard in range can
        /// take the nearest. Replaces the guards' own alarm subscription (#205).</summary>
        private void RaiseHueAndCry()
        {
            for (int i = 0; i < _intruders.Count; i++)
            {
                if (_intruders[i] != null)
                    Publish(new InvestigateRequest(_intruders[i].position, InvestigateReason.HueAndCry));
            }
        }

        [Header("Guard navigation")]
        [SerializeField] private GuardNavigationTuning _navigationTuning = new GuardNavigationTuning();

        private GuardNavigationService _navigation;

        /// <summary>Moves guards on request (#222). Made on first use so EditMode tests need no Awake.
        /// Give it a map with <see cref="GuardNavigationService.SetMap"/> before sending requests.</summary>
        public GuardNavigationService Navigation => _navigation ??= new GuardNavigationService(this, _navigationTuning);

        private void Update()
        {
            // Only the server integrates decay; clients receive state via replication.
            if (isSpawned && !isServer)
                return;
            TickDecay(Time.deltaTime);
            _navigation?.Tick(Time.deltaTime);
        }

        // ---------------------------------------------------------------------------------------
        // Noise intake
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// <see cref="INoiseListener"/> entry point. On a client this forwards to the server; on the
        /// server (or in single-player / tests) it applies the noise directly.
        /// </summary>
        public void OnNoiseHeard(NoiseEvent noise)
        {
            if (isSpawned && !isServer)
            {
                ReportNoiseServer(noise.Origin, noise.Strength, (int)noise.Type);
                return;
            }
            Publish(new NoiseReported(noise.Origin, noise.Strength));
        }

        [ServerRpc(requireOwnership: false)]
        private void ReportNoiseServer(Vector3 origin, float strength, int type)
        {
            Publish(new NoiseReported(origin, strength));
        }

        /// <summary>Pure noise application: bump the level, stamp the time and re-evaluate state.</summary>
        public void ApplyNoise(float strength) => Raise(strength * _noiseWeight);

        /// <summary>A guard has spotted an intruder and given chase. Server-side.</summary>
        public void ReportSighting() => Raise(_sightingPoints);

        /// <summary>A guard has attacked a player. Server-side.</summary>
        public void ReportAttack() => Raise(_attackPoints);

        /// <summary>
        /// A guard started (<paramref name="chasing"/> true) or stopped chasing. Enough guards on the
        /// chase at once force the castle to Roused, then Hue and Cry. Stopping never lowers it.
        /// </summary>
        public void ReportChase(int guardId, bool chasing)
        {
            bool changed = chasing ? _chasers.Add(guardId) : _chasers.Remove(guardId);
            if (!changed || !chasing || InGrace)
                return;

            float floor = _chasers.Count >= _hueAndCryChasers ? HueAndCryThreshold
                : _chasers.Count >= _rousedChasers ? RousedThreshold
                : 0f;
            if (floor > _alarmLevel.value)
                Raise(floor - _alarmLevel.value);
        }

        /// <summary>
        /// A new raid: Calm, level 0, the latch released, no chasers, and nothing raises the alarm
        /// for <paramref name="graceSeconds"/>. Without the latch release a raid that ended in Hue
        /// and Cry started the next one in it (#136).
        /// </summary>
        public void ResetForNewRaid(float graceSeconds)
        {
            _locked = false;
            _chasers.Clear();
            _alarmLevel.value = 0f;
            _lastNoiseTime = Time.time;
            _graceEndsAt = Time.time + Mathf.Max(0f, graceSeconds);
            UpdateState();
        }

        private void Raise(float points)
        {
            if (points <= 0f || InGrace)
                return;

            _alarmLevel.value = Mathf.Clamp(_alarmLevel.value + points, 0f, 100f);
            _lastNoiseTime = Time.time;
            UpdateState();
        }

        // ---------------------------------------------------------------------------------------
        // Decay + state evaluation
        // ---------------------------------------------------------------------------------------

        /// <summary>Advances decay by <paramref name="deltaTime"/> seconds (network-free, testable).</summary>
        public void TickDecay(float deltaTime)
        {
            if (!_locked && Time.time - _lastNoiseTime > _decayDelay)
                _alarmLevel.value -= _decayRate * deltaTime;

            _alarmLevel.value = Mathf.Clamp(_alarmLevel.value, 0f, 100f);
            UpdateState();
        }

        /// <summary>Test/setup helper: force the alarm level and immediately re-evaluate state.</summary>
        public void SetAlarmLevel(float level)
        {
            _alarmLevel.value = Mathf.Clamp(level, 0f, 100f);
            UpdateState();
        }

        /// <summary>
        /// Maps the current level to an <see cref="AlarmState"/>. Once locked (Roused/HueAndCry), the
        /// state can only escalate — a falling level never regresses it.
        /// </summary>
        public void UpdateState()
        {
            AlarmState computed = ComputeState(_alarmLevel.value);

            if (_locked && computed < _alarmState.value)
                computed = _alarmState.value;

            if (computed >= AlarmState.Roused)
                _locked = true;

            SetState(computed);
        }

        private static AlarmState ComputeState(float level)
        {
            if (level >= HueAndCryThreshold) return AlarmState.HueAndCry;
            if (level >= RousedThreshold) return AlarmState.Roused;
            if (level >= StirredThreshold) return AlarmState.Stirred;
            return AlarmState.Calm;
        }

        private void SetState(AlarmState newState)
        {
            if (_alarmState.value == newState)
                return;

            _alarmState.value = newState;

            // The hue and cry is a request, not an order: guards decide what to do with it.
            if (newState == AlarmState.HueAndCry && IsAuthority)
                RaiseHueAndCry();

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
            OnAlarmChanged?.Invoke(new AlarmChanged(newState));
        }
    }
}

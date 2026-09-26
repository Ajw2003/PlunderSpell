using System;
using System.Collections.Generic;
using PurrNet;
using RogueAi.Acoustics;
using UnityEngine;

namespace RogueAi.Alarm
{
    /// <summary>
    /// Server-authoritative four-state alarm state machine. It listens for noise (as an
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
    public class AlarmFSMManager : NetworkBehaviour, INoiseListener
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

        private void Update()
        {
            // Only the server integrates decay; clients receive state via replication.
            if (isSpawned && !isServer)
                return;
            TickDecay(Time.deltaTime);
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
            ApplyNoise(noise.Strength);
        }

        [ServerRpc(requireOwnership: false)]
        private void ReportNoiseServer(Vector3 origin, float strength, int type)
        {
            ApplyNoise(strength);
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

            if (isSpawned && isServer)
                BroadcastAlarmState(newState);
            else
                RaiseStateChanged(newState); // single-player / EditMode tests
        }

        /// <summary>Fans the new state out to every observer and raises the local event on each.</summary>
        [ObserversRpc(bufferLast: true)]
        private void BroadcastAlarmState(AlarmState newState) => RaiseStateChanged(newState);

        private void RaiseStateChanged(AlarmState newState) => AlarmStateChanged?.Invoke(newState);
    }
}

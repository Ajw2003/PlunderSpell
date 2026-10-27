using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The alarm (formerly AlarmFSMManager, folded into the director by the owner's decision): a
    /// server-authoritative four-state machine over a level of 0 to 100. Noise, sightings, attacks and
    /// guards on the chase raise the level; it bleeds off at <see cref="AlarmTuning.DecayRate"/> per second
    /// once nothing has raised it for <see cref="AlarmTuning.DecayDelay"/> seconds, while Calm or Stirred.
    /// Reaching Roused or Hue and Cry latches <see cref="IsLocked"/>: from then on the alarm never decays
    /// and the state can only escalate, until <see cref="ResetForNewRaid"/>.
    ///
    /// The level and state are the director's SyncVars (a SyncVar must be a field of a NetworkBehaviour),
    /// handed in here. A change of state is reported through <c>stateChanged</c>; the director fans it out.
    /// Network-free, so EditMode tests drive it directly.
    /// </summary>
    public sealed class DirectorAlarm
    {
        private const float StirredThreshold = 20f;
        private const float RousedThreshold = 50f;
        private const float HueAndCryThreshold = 80f;

        private readonly SyncVar<float> _level;
        private readonly SyncVar<AlarmState> _state;
        private readonly AlarmTuning _tuning;
        private readonly Action<AlarmState> _stateChanged;
        private readonly HashSet<int> _chasers = new HashSet<int>(); // guards chasing right now
        private readonly HashSet<int> _witnesses = new HashSet<int>(); // guards that have seen an intruder this raid
        private readonly Dictionary<int, float> _lastNoiseReport = new Dictionary<int, float>(); // guard -> time of its last scored report

        /// <summary>Seconds a guard waits before its next heard noise scores again.</summary>
        public const float GuardNoiseReportInterval = 2f;

        private float _lastNoiseTime;   // server-only timestamp of the most recent noise
        private float _graceEndsAt;     // server-only: nothing raises the alarm before this time

        public DirectorAlarm(SyncVar<float> level, SyncVar<AlarmState> state, AlarmTuning tuning, Action<AlarmState> stateChanged)
        {
            _level = level;
            _state = state;
            _tuning = tuning;
            _stateChanged = stateChanged;
        }

        public float Level => _level.value;
        public AlarmState State => _state.value;
        public bool IsLocked { get; private set; }
        public bool InGrace => Time.time < _graceEndsAt;
        public int ChasingGuards => _chasers.Count;

        /// <summary>Starts the decay delay from now, as when the director spawns.</summary>
        public void NoteNoiseNow() => _lastNoiseTime = Time.time;

        /// <summary>How many distinct guards have seen an intruder this raid; the Roused and Hue and Cry gates read it.</summary>
        public int Witnesses => _witnesses.Count;

        /// <summary>A guard heard an intruder's noise (#259). It scores once per guard, and a guard reports at
        /// most once every <see cref="GuardNoiseReportInterval"/> seconds, so a castle scores what its guards hear.</summary>
        public void ReportHeardNoise(int guardId, float strength)
        {
            if (_lastNoiseReport.TryGetValue(guardId, out float last) && Time.time - last < GuardNoiseReportInterval)
                return;

            _lastNoiseReport[guardId] = Time.time;
            Raise(strength * _tuning.NoiseWeight);
        }

        /// <summary>A guard's first sighting: points, and the guard becomes a witness.</summary>
        public void ReportSighting(int guardId)
        {
            AddWitness(guardId);
            Raise(_tuning.SightingPoints);
        }

        public void ReportAttack() => Raise(_tuning.AttackPoints);

        // A witness is added before the points so the state check in Raise sees it. Nothing counts in the grace.
        private void AddWitness(int guardId)
        {
            if (!InGrace && _witnesses.Add(guardId))
                UpdateState();
        }

        /// <summary>A guard started (<paramref name="chasing"/> true) or stopped chasing. Enough guards on the
        /// chase at once force the castle to Roused, then Hue and Cry. Stopping never lowers it.</summary>
        public void ReportChase(int guardId, bool chasing)
        {
            bool changed = chasing ? _chasers.Add(guardId) : _chasers.Remove(guardId);
            if (!changed || !chasing || InGrace)
                return;

            // A chaser has seen an intruder, so the floors below never outrun the witness gates.
            AddWitness(guardId);

            float floor = _chasers.Count >= _tuning.HueAndCryChasers ? HueAndCryThreshold
                : _chasers.Count >= _tuning.RousedChasers ? RousedThreshold
                : 0f;
            if (floor > _level.value)
                Raise(floor - _level.value);
        }

        /// <summary>A new raid: Calm, level 0, the latch released, no chasers, and nothing raises the alarm
        /// for <paramref name="graceSeconds"/>. Without the latch release a raid that ended in Hue and Cry
        /// started the next one in it (#136).</summary>
        public void ResetForNewRaid(float graceSeconds)
        {
            IsLocked = false;
            _chasers.Clear();
            _witnesses.Clear();
            _lastNoiseReport.Clear();
            _level.value = 0f;
            _lastNoiseTime = Time.time;
            _graceEndsAt = Time.time + Mathf.Max(0f, graceSeconds);
            UpdateState();
        }

        /// <summary>Advances decay by <paramref name="deltaTime"/> seconds.</summary>
        public void TickDecay(float deltaTime)
        {
            if (!IsLocked && Time.time - _lastNoiseTime > _tuning.DecayDelay)
                _level.value -= _tuning.DecayRate * deltaTime;

            _level.value = Mathf.Clamp(_level.value, 0f, 100f);
            UpdateState();
        }

        /// <summary>Forces the level and re-evaluates the state. For tests and setup. Witness gates still apply;
        /// <paramref name="witnesses"/> adds that many stand-in witnesses first, for a test that wants Roused or above.</summary>
        public void SetLevel(float level, int witnesses = 0)
        {
            for (int i = 0; i < witnesses; i++)
                _witnesses.Add(-1 - i);
            _level.value = Mathf.Clamp(level, 0f, 100f);
            UpdateState();
        }

        /// <summary>Maps the level to a state. Once locked the state can only escalate: a falling level never regresses it.</summary>
        public void UpdateState()
        {
            AlarmState computed = ComputeState(_level.value);

            if (IsLocked && computed < _state.value)
                computed = _state.value;

            if (computed >= AlarmState.Roused)
                IsLocked = true;

            if (_state.value == computed)
                return;

            _state.value = computed;
            _stateChanged(computed);
        }

        private void Raise(float points)
        {
            if (points <= 0f || InGrace)
                return;

            _level.value = Mathf.Clamp(_level.value + points, 0f, 100f);
            _lastNoiseTime = Time.time;
            UpdateState();
        }

        // Points keep piling up while a witness gate is unmet; the state waits for the witnesses.
        private AlarmState ComputeState(float level)
        {
            if (level >= HueAndCryThreshold && _witnesses.Count >= _tuning.HueAndCryWitnesses) return AlarmState.HueAndCry;
            if (level >= RousedThreshold && _witnesses.Count >= _tuning.RousedWitnesses) return AlarmState.Roused;
            if (level >= StirredThreshold) return AlarmState.Stirred;
            return AlarmState.Calm;
        }
    }
}

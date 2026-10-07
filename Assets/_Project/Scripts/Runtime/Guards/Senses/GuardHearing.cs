using System;
using Plunderspell.Acoustics;
using Plunderspell.Alarm;
using Plunderspell.Status;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// What a guard hears. A loud noise wakes a sleeper (Somnus buys time, it does not remove a patrol),
    /// and a noise above the alarm's threshold is announced as <see cref="NoiseNoticed"/>. It decides
    /// and raises an event; the current state decides what the guard does about it.
    /// </summary>
    public sealed class GuardHearing
    {
        /// <summary>Noise at or above this strength wakes a sleeping guard.</summary>
        public const float WakeThreshold = 0.5f;

        private readonly StatusEffectReceiver _status;
        private bool _closed;

        /// <summary>Raised with where the noise came from and how strong it was, when it is worth a look.</summary>
        public event Action<Vector3, float> NoiseNoticed;

        /// <summary>Raised with where another guard cried out, when this guard (awake) heard it.</summary>
        public event Action<Vector3> CryHeard;

        /// <summary>Raised for every intruder noise this guard heard while awake, worth a look or not: the
        /// castle scores what its guards hear (#259).</summary>
        public event Action<Vector3, float> NoiseHeard;

        public GuardHearing(StatusEffectReceiver status)
        {
            _status = status;
        }

        /// <summary>How many noises this guard has found worth a look.</summary>
        public int NoticedCount { get; private set; }

        /// <summary>Stops listening for good (death, #213): a dead guard is not woken or alerted by noise.</summary>
        public void Close() => _closed = true;

        /// <summary>A noise reached the guard: wake it if loud enough, then announce it if the alarm level says it matters.</summary>
        public void Hear(NoiseEvent noise, AlarmState alarm)
        {
            if (_closed)
                return;

            // A cry is the guards talking: it neither wakes a sleeper nor counts as a noise to report.
            if (noise.Type == NoiseType.GuardCry)
            {
                if (_status == null || !_status.IsAsleep)
                    CryHeard?.Invoke(noise.Origin);
                return;
            }

            if (_status != null && _status.IsAsleep && noise.Strength >= WakeThreshold)
                _status.WakeUp();

            // A noise too quiet to wake the guard goes unheard; it must not wait as a lead for when it wakes.
            if (_status != null && _status.IsAsleep)
                return;

            NoiseHeard?.Invoke(noise.Origin, noise.Strength);

            if (!GuardBrain.ShouldInvestigate(noise.Strength, alarm))
                return;

            NoticedCount++;
            NoiseNoticed?.Invoke(noise.Origin, noise.Strength);
        }
    }
}

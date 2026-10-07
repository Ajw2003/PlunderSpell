using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>Why the director is asking guards to investigate a spot.</summary>
    public enum InvestigateReason
    {
        /// <summary>The alarm reached Hue and Cry: head for a player.</summary>
        HueAndCry,

        /// <summary>A spot worth a look that is not the hue and cry: only nearby guards take it up.</summary>
        Noise
    }

    // The director's event payloads. Plain readonly structs so raising one never allocates, and so the
    // Alarm assembly needs no reference to Guards: a guard is carried as a Component.

    /// <summary>A guard heard an intruder's noise, with where, how strong, and which guard (#259).</summary>
    public readonly struct NoiseReported
    {
        public readonly Component Guard;
        public readonly Vector3 Origin;
        public readonly float Strength;
        public NoiseReported(Component guard, Vector3 origin, float strength)
        {
            Guard = guard; Origin = origin; Strength = strength;
        }
    }

    /// <summary>A guard saw an intruder and gave chase.</summary>
    public readonly struct IntruderSpotted
    {
        public readonly Component Guard;
        public readonly Transform Intruder;
        public readonly Vector3 Position;
        /// <summary>True the first time this guard spots someone in a chase; only that scores alarm points.</summary>
        public readonly bool FirstSighting;
        public IntruderSpotted(Component guard, Transform intruder, Vector3 position, bool firstSighting = true)
        {
            Guard = guard; Intruder = intruder; Position = position; FirstSighting = firstSighting;
        }
    }

    /// <summary>A guard stopped chasing.</summary>
    public readonly struct IntruderLost
    {
        public readonly Component Guard;
        public readonly Vector3 LastKnownPosition;
        public IntruderLost(Component guard, Vector3 lastKnownPosition)
        {
            Guard = guard; LastKnownPosition = lastKnownPosition;
        }
    }

    /// <summary>A guard struck or fired at a player.</summary>
    public readonly struct GuardEngaged
    {
        public readonly Component Guard;
        public readonly Transform Target;
        public GuardEngaged(Component guard, Transform target) { Guard = guard; Target = target; }
    }

    /// <summary>The alarm state changed.</summary>
    public readonly struct AlarmChanged
    {
        public readonly AlarmState State;
        public AlarmChanged(AlarmState state) { State = state; }
    }

    /// <summary>A guard died.</summary>
    public readonly struct GuardDied
    {
        public readonly Component Guard;
        public readonly Vector3 Position;
        public GuardDied(Component guard, Vector3 position) { Guard = guard; Position = position; }
    }

    /// <summary>A guard sees a player it cannot reach (on a table, a ledge, a rail) and asks for help (#237).
    /// Nearby guards decide what to do with it: ranged ones come to shoot, melee ones go and hold below.</summary>
    public readonly struct UnreachableIntruderReported
    {
        public readonly Component Guard;
        public readonly Transform Intruder;
        public UnreachableIntruderReported(Component guard, Transform intruder) { Guard = guard; Intruder = intruder; }
    }

    /// <summary>A request that guards investigate <see cref="Position"/>. Guards' states decide whether
    /// and how to act on it; nothing outside a guard moves it.</summary>
    public readonly struct InvestigateRequest
    {
        public readonly Vector3 Position;
        public readonly InvestigateReason Reason;
        public InvestigateRequest(Vector3 position, InvestigateReason reason)
        {
            Position = position; Reason = reason;
        }
    }
}

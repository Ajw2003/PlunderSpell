using System;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>Numbers the navigation service moves guards by. Plain fields so the director can show them in the Inspector.</summary>
    [Serializable]
    public sealed class GuardNavigationTuning
    {
        [Tooltip("Layers the sweep stops for: walls and players. Keep guards' own layer out of it; they keep apart by spacing.")]
        public LayerMask SweepMask = Physics.DefaultRaycastLayers;

        [Tooltip("Ledges up to this high are stepped over by the sweep, so stair risers do not stop a guard. Two risers: the body's front edge meets the next riser while its centre is still a step lower.")]
        public float StepHeight = 0.7f;

        [Tooltip("Gap the guard keeps from whatever the sweep hit, in metres.")]
        public float SkinWidth = 0.03f;

        [Tooltip("Guards are pushed apart until their centres are this far from each other.")]
        public float SeparationSpacing = 1.0f;

        [Tooltip("Top speed of the separation push, as a share of the guard's own speed.")]
        public float SeparationSpeedShare = 0.8f;

        [Tooltip("A waypoint counts as reached inside this horizontal distance.")]
        public float WaypointRadius = 0.2f;

        [Tooltip("The destination counts as reached inside this horizontal distance.")]
        public float ArriveRadius = 0.3f;

        [Tooltip("Seconds of being held up by the sweep before the move is reported Blocked.")]
        public float BlockedSeconds = 0.5f;

        [Tooltip("Share of the step that must get through the sweep to count as not held up.")]
        public float ProgressShare = 0.25f;

        [Tooltip("How fast a guard climbs or drops to the floor height, in metres per second.")]
        public float VerticalSpeed = 8f;

        [Tooltip("How fast a walking guard turns to face where it is going, in degrees per second.")]
        public float TurnDegreesPerSecond = 270f;

        [Tooltip("How far the guard's pivot sits above the floor. Zero when the pivot is at the feet.")]
        public float PivotAboveFloor = 0f;
    }
}

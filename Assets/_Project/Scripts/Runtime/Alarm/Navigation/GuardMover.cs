using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// What the navigation service tracks for one guard: its body shape, the move it was last asked
    /// for, and where it is along the route. Owned and written by the service; one per registered guard.
    /// </summary>
    public sealed class GuardMover
    {
        public readonly Component Guard;
        public readonly Transform Body;
        public readonly float Radius;
        public readonly float Height;

        public float Speed;
        public MoveReason Reason;
        public Vector3 Destination;

        /// <summary>Smoothed route, shared with the cache and never edited. Null when the guard is standing.</summary>
        public Vector3[] Path;
        public int NextWaypoint;

        /// <summary>Seconds the sweep has held this guard to less than <see cref="GuardNavigationTuning.ProgressShare"/> of its step.</summary>
        public float HeldUpSeconds;

        public GuardMover(Component guard, float radius, float height)
        {
            Guard = guard;
            Body = guard.transform;
            Radius = radius;
            Height = height;
        }

        public bool IsMoving => Path != null;

        public Vector3 Position => Body.position;

        public void Stop()
        {
            Path = null;
            NextWaypoint = 0;
            HeldUpSeconds = 0f;
        }
    }
}

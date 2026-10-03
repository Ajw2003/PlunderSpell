using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Works out where along its route a guard wants to go this tick. It only decides the step; the
    /// stepper decides how much of it is allowed. Heights are ignored here: the guard walks the
    /// horizontal plane and the stepper settles it onto the floor.
    /// </summary>
    public sealed class GuardPathFollower
    {
        private readonly GuardNavigationTuning _tuning;

        public GuardPathFollower(GuardNavigationTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>The horizontal step toward the next waypoint, never longer than speed times time.</summary>
        public Vector3 NextStep(GuardMover mover, float deltaTime)
        {
            SkipReachedWaypoints(mover);
            Vector3 toWaypoint = mover.Path[mover.NextWaypoint] - mover.Position;
            toWaypoint.y = 0f;
            return Vector3.ClampMagnitude(toWaypoint, mover.Speed * deltaTime);
        }

        /// <summary>True once the guard is on its last waypoint and close enough to call it there.</summary>
        public bool HasArrived(GuardMover mover)
        {
            if (mover.NextWaypoint < mover.Path.Length - 1)
                return false;
            return HorizontalDistance(mover.Position, mover.Path[mover.Path.Length - 1]) <= _tuning.ArriveRadius;
        }

        // Never skips the last waypoint: arriving there is what ends the move.
        private void SkipReachedWaypoints(GuardMover mover)
        {
            int last = mover.Path.Length - 1;
            while (mover.NextWaypoint < last
                   && HorizontalDistance(mover.Position, mover.Path[mover.NextWaypoint]) <= _tuning.WaypointRadius)
                mover.NextWaypoint++;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}

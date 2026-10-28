using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The fallback for a guard whose route to a target failed (#481): walk to the floor nearest the
    /// target that the guard can reach, the long way round, instead of standing still or giving up.
    /// Only a missing route counts; a closed door is left to the door logic.
    /// </summary>
    public sealed class GuardNearestReachableWalk
    {
        // Closer than this to the guard already: walking there would change nothing.
        private const float AlreadyThereDistance = 1f;

        private readonly Guard _guard;

        public GuardNearestReachableWalk(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>True when the failure was a missing route rather than a door or a sweep that was held up.</summary>
        public static bool IsMissingRoute(Blocked blocked)
        {
            return blocked.Reason == BlockedReason.Unreachable || blocked.Reason == BlockedReason.NoWalkableCell;
        }

        /// <summary>Starts the walk and returns true, or returns false when there is no reachable floor near the target or the guard already stands on it.</summary>
        public bool TryStart(Vector3 target, float speed, MoveReason reason)
        {
            IGuardNavigationMap map = _guard.Link.Director != null ? _guard.Link.Director.Navigation.Map : null;
            if (map == null || !map.TryFindNearestReachable(_guard.transform.position, target, out Vector3 reachable))
                return false;
            if (Vector3.Distance(_guard.transform.position, reachable) < AlreadyThereDistance)
                return false;

            _guard.Navigator.MoveTo(reachable, speed, reason);
            return true;
        }
    }
}

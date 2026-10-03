using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Decides from the navigation map whether a player can be got at on foot (#237). The map has no
    /// cells on tables or rails, so a player standing on one has either no floor near them or a floor far
    /// below their feet. A player the guard can already hit (within melee reach) always counts as reachable.
    /// The answer must hold for <c>UnreachableConfirmSeconds</c> before it counts, so a jump does not turn
    /// a chasing guard into a stone thrower. Without a director or a map nothing is unreachable.
    /// </summary>
    public sealed class GuardReachability
    {
        private readonly Guard _guard;
        private float _secondsUnreachable;

        public GuardReachability(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>Counts time while <paramref name="target"/> cannot be reached and returns true once it has
        /// been that way for the confirm time. Call once per tick while the target is in sight.</summary>
        public bool IsUnreachable(Transform target, float deltaTime)
        {
            _secondsUnreachable = CanReach(target) ? 0f : _secondsUnreachable + deltaTime;
            return _secondsUnreachable >= _guard.Tuning.UnreachableConfirmSeconds;
        }

        /// <summary>Forgets the time counted, for a guard that has stopped dealing with this player.</summary>
        public void Reset() => _secondsUnreachable = 0f;

        private bool CanReach(Transform target)
        {
            GuardTuning tuning = _guard.Tuning;
            Vector3 spot = target.position;
            if (Vector3.Distance(_guard.transform.position, spot) <= tuning.MeleeReach)
                return true;

            Alarm.IGuardNavigationMap map = _guard.Link.Director?.Navigation.Map;
            if (map == null)
                return true;

            if (!map.TryGetFloorHeight(spot, out float floorHeight))
                return false;
            return spot.y + tuning.TargetLowAimHeight - floorHeight <= tuning.UnreachableHeight;
        }
    }
}

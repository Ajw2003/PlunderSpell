using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Answers "is a levitated guard back on the ground". Levo lifts the guard and something other than the
    /// navigation service (the spell's own push and gravity) carries it, so the guard is down once the spell
    /// has ended and its pivot is within <see cref="GuardTuning.LandingTolerance"/> of the floor the
    /// director's map reports. Landing does no damage: the legacy fall damage is dropped (docs/plans/guard-core-inventory.md).
    /// </summary>
    public sealed class GuardLanding
    {
        private readonly Guard _guard;

        public GuardLanding(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>True when the spell is over and the guard is on, or close enough to, the floor. With no map to ask, the end of the spell is landing.</summary>
        public bool HasLanded
        {
            get
            {
                if (_guard.Status.IsLevitating || _guard.Lift.IsAirborne)
                    return false;

                EnemyDirector director = _guard.Link.Director;
                IGuardNavigationMap map = director != null ? director.Navigation.Map : null;
                Vector3 position = _guard.transform.position;
                if (map == null || !map.TryGetFloorHeight(position, out float floorHeight))
                    return true;

                float pivotHeight = floorHeight + director.Navigation.Tuning.PivotAboveFloor;
                return position.y - pivotHeight <= _guard.Tuning.LandingTolerance;
            }
        }
    }
}

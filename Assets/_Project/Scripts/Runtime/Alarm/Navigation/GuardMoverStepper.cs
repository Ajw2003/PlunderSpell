using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Applies one tick of movement to a guard by setting its transform: the horizontal step cut down by
    /// the capsule sweep, then the height settled onto the floor from the map. No Rigidbody and no
    /// agent, so nothing here can push a player; the sweep is the only thing that decides how far the
    /// guard goes.
    /// </summary>
    public sealed class GuardMoverStepper
    {
        private const float MinimumStep = 0.00001f;

        private readonly GuardNavigationTuning _tuning;
        private readonly GuardSweep _sweep;

        public GuardMoverStepper(GuardNavigationTuning tuning, GuardSweep sweep)
        {
            _tuning = tuning;
            _sweep = sweep;
        }

        /// <summary>
        /// Moves the guard by <paramref name="step"/> (horizontal) as far as the sweep allows, then
        /// settles its height. Returns the share of the step that got through, 1 for an empty step.
        /// </summary>
        public float Move(GuardMover mover, Vector3 step, IGuardNavigationMap map, float deltaTime)
        {
            float share = 1f;
            float distance = step.magnitude;
            Vector3 position = mover.Position;
            if (distance > MinimumStep)
            {
                Vector3 direction = step / distance;
                float allowed = _sweep.AllowedDistance(mover, direction, distance);
                position += direction * allowed;
                share = allowed / distance;
            }
            position.y = SettledHeight(position, mover.Position.y, map, deltaTime);
            mover.Body.position = position;
            return share;
        }

        // Rises or falls toward the floor under the new spot, a little each tick, so stairs read as a climb.
        private float SettledHeight(Vector3 position, float currentHeight, IGuardNavigationMap map, float deltaTime)
        {
            if (map == null || !map.TryGetFloorHeight(position, out float floorHeight))
                return currentHeight;
            return Mathf.MoveTowards(currentHeight, floorHeight + _tuning.PivotAboveFloor, _tuning.VerticalSpeed * deltaTime);
        }
    }
}

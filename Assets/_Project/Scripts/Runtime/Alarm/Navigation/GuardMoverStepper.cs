using Interfaces;
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
        /// Moves the guard by <paramref name="step"/> (horizontal) as far as the sweep allows, sliding
        /// along whatever cut it short, then settles its height. Returns the share of the step's length
        /// that was made good in the step's own direction, 1 for an empty step.
        /// </summary>
        public float Move(GuardMover mover, Vector3 step, IGuardNavigationMap map, float deltaTime)
        {
            float share = 1f;
            float distance = step.magnitude;
            Vector3 start = mover.Position;
            Vector3 position = start;
            if (distance > MinimumStep)
            {
                Vector3 direction = step / distance;
                position = SweptMove(mover, position, direction, distance, out Vector3 hitNormal, out Collider blocker);
                if (OpenDoorIn(blocker))
                    position = SweptMove(mover, start, direction, distance, out hitNormal, out _);
                float remaining = distance - Vector3.Distance(start, position);
                if (remaining > MinimumStep && hitNormal != Vector3.zero)
                    position = Slide(mover, position, direction, remaining, hitNormal);
                share = Vector3.Dot(position - start, direction) / distance;
            }
            position.y = SettledHeight(position, start.y, map, deltaTime);
            mover.Body.position = position;
            return share;
        }

        // Guards are castle staff with keys: a closed door that cut the step short is opened (unlocked or
        // locked alike); a barred one stays shut. Without this a guard routed through a door stalls on it.
        private static bool OpenDoorIn(Collider blocker)
        {
            IHandOpenable door = blocker != null ? blocker.GetComponentInParent<IHandOpenable>() : null;
            if (door == null || door.IsOpen || door.IsBarred)
                return false;
            door.Open();
            Physics.SyncTransforms();
            return true;
        }

        private Vector3 SweptMove(GuardMover mover, Vector3 from, Vector3 direction, float distance, out Vector3 hitNormal, out Collider blocker)
        {
            float allowed = _sweep.AllowedDistance(mover, from, direction, distance, out hitNormal, out blocker);
            return from + direction * allowed;
        }

        // Grazing an archway's edge or a cart's corner turns into a slide past it instead of a dead stop.
        // Only the part of the step along the surface is kept, so a guard walking square into a wall stays put
        // and is still reported held up. Sliding past a player moves only the guard, never the player (#200).
        private Vector3 Slide(GuardMover mover, Vector3 from, Vector3 direction, float remaining, Vector3 hitNormal)
        {
            Vector3 along = Vector3.ProjectOnPlane(direction, new Vector3(hitNormal.x, 0f, hitNormal.z).normalized);
            along.y = 0f;
            float slideDistance = remaining * along.magnitude;
            if (slideDistance <= MinimumStep)
                return from;
            return SweptMove(mover, from, along.normalized, slideDistance, out _, out _);
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

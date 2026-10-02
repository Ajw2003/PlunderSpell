using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The capsule sweep that keeps the #200 wall-crush fix once guards have no physics body
    /// (docs/plans/issue-200-collision-netcode.md). Before a step the guard's capsule is swept along it;
    /// the step is cut short of the first wall or player hit, with a small skin left, so a guard can
    /// never be moved into a player, or move a player into a wall. The hit buffer is allocated once.
    /// </summary>
    public sealed class GuardSweep
    {
        private const int HitBufferSize = 16;

        private readonly GuardNavigationTuning _tuning;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        public GuardSweep(GuardNavigationTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>
        /// How far the mover may go along <paramref name="direction"/> (unit length), up to
        /// <paramref name="distance"/>. Triggers and the mover's own colliders are ignored.
        /// </summary>
        public float AllowedDistance(GuardMover mover, Vector3 direction, float distance)
        {
            // The bottom sphere starts a step above the floor so stair risers and low clutter do not stop it.
            Vector3 position = mover.Position;
            Vector3 bottom = position + Vector3.up * (_tuning.StepHeight + mover.Radius);
            Vector3 top = position + Vector3.up * Mathf.Max(mover.Height - mover.Radius, _tuning.StepHeight + mover.Radius);

            int count = Physics.CapsuleCastNonAlloc(bottom, top, mover.Radius, direction, _hits,
                distance + _tuning.SkinWidth, _tuning.SweepMask, QueryTriggerInteraction.Ignore);

            float nearest = distance + _tuning.SkinWidth;
            for (int i = 0; i < count; i++)
            {
                if (BlocksStep(_hits[i], mover, direction) && _hits[i].distance < nearest)
                    nearest = _hits[i].distance;
            }
            return Mathf.Clamp(nearest - _tuning.SkinWidth, 0f, distance);
        }

        // A hit at distance zero means the capsule already overlaps that collider; it only blocks if
        // the step goes further into it, otherwise a guard resting against a wall could never leave.
        private static bool BlocksStep(RaycastHit hit, GuardMover mover, Vector3 direction)
        {
            if (hit.collider.transform.IsChildOf(mover.Body))
                return false;
            return hit.distance > 0f || Vector3.Dot(hit.normal, direction) < -0.01f;
        }
    }
}

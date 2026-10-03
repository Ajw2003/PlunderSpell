using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The capsule sweep that keeps the #200 wall-crush fix once guards have no physics body: a step is cut
    /// short of the first wall or player hit, so a guard can never be moved into a player. Other guards'
    /// bodies are ignored (<see cref="GuardSeparation"/> keeps guards apart); why: docs/4-systems/alarm.md,
    /// Guard navigation. The hit buffer is allocated once.
    /// </summary>
    public sealed class GuardSweep
    {
        private const int HitBufferSize = 16;

        private readonly GuardNavigationTuning _tuning;
        private readonly HashSet<Transform> _guardBodies;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];

        /// <param name="guardBodies">Every registered guard's transform, kept current by the navigation service.</param>
        public GuardSweep(GuardNavigationTuning tuning, HashSet<Transform> guardBodies)
        {
            _tuning = tuning;
            _guardBodies = guardBodies;
        }

        /// <summary>
        /// How far the mover may go from <paramref name="from"/> along <paramref name="direction"/> (unit
        /// length), up to <paramref name="distance"/>. <paramref name="hitNormal"/> is the surface that cut
        /// the step short, or zero when nothing did. Triggers and guards' own colliders are ignored.
        /// </summary>
        public float AllowedDistance(GuardMover mover, Vector3 from, Vector3 direction, float distance, out Vector3 hitNormal)
        {
            // The bottom sphere starts a step above the floor so stair risers and low clutter do not stop it.
            Vector3 bottom = from + Vector3.up * (_tuning.StepHeight + mover.Radius);
            Vector3 top = from + Vector3.up * Mathf.Max(mover.Height - mover.Radius, _tuning.StepHeight + mover.Radius);

            int count = Physics.CapsuleCastNonAlloc(bottom, top, mover.Radius, direction, _hits,
                distance + _tuning.SkinWidth, _tuning.SweepMask, QueryTriggerInteraction.Ignore);

            float nearest = distance + _tuning.SkinWidth;
            hitNormal = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                if (BlocksStep(_hits[i], direction) && _hits[i].distance < nearest)
                {
                    nearest = _hits[i].distance;
                    hitNormal = _hits[i].normal;
                }
            }
            return Mathf.Clamp(nearest - _tuning.SkinWidth, 0f, distance);
        }

        // A hit at distance zero means the capsule already overlaps that collider; it only blocks if
        // the step goes further into it, otherwise a guard resting against a wall could never leave.
        private bool BlocksStep(RaycastHit hit, Vector3 direction)
        {
            if (IsGuardBody(hit.collider.transform))
                return false;
            return hit.distance > 0f || Vector3.Dot(hit.normal, direction) < -0.01f;
        }

        // Walks up the hierarchy rather than reading the root: spawned guards sit under a shared container.
        private bool IsGuardBody(Transform part)
        {
            for (Transform current = part; current != null; current = current.parent)
            {
                if (_guardBodies.Contains(current))
                    return true;
            }
            return false;
        }
    }
}

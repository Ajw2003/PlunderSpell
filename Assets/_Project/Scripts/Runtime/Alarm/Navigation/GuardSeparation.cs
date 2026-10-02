using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Keeps guards from stacking. Every guard closer than the spacing to another is pushed straight
    /// away from it, harder the closer it is. Standing guards are pushed too, so a crowd that has
    /// arrived at one spot spreads out instead of sitting on top of each other.
    /// </summary>
    public sealed class GuardSeparation
    {
        private readonly GuardNavigationTuning _tuning;

        public GuardSeparation(GuardNavigationTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>How far this guard should be nudged this tick, horizontally.</summary>
        public Vector3 PushFor(GuardMover mover, List<GuardMover> movers, float deltaTime)
        {
            Vector3 push = Vector3.zero;
            Vector3 position = mover.Position;
            for (int i = 0; i < movers.Count; i++)
            {
                if (movers[i] != mover)
                    push += AwayFrom(position, movers[i].Position, mover);
            }
            float maxStep = Mathf.Max(mover.Speed, 1f) * _tuning.SeparationSpeedShare * deltaTime;
            return Vector3.ClampMagnitude(push * maxStep, maxStep);
        }

        // Full strength at zero distance, fading to nothing at the spacing.
        private Vector3 AwayFrom(Vector3 position, Vector3 other, GuardMover mover)
        {
            Vector3 offset = position - other;
            offset.y = 0f;
            float distance = offset.magnitude;
            if (distance >= _tuning.SeparationSpacing)
                return Vector3.zero;
            if (distance < 0.001f)
                return FallbackDirection(mover);
            return offset / distance * (1f - distance / _tuning.SeparationSpacing);
        }

        // Two guards on exactly the same point have no "away"; each picks a direction from its own id,
        // so they split rather than staying put.
        private static Vector3 FallbackDirection(GuardMover mover)
        {
            float angle = (mover.Guard.GetInstanceID() & 1023) * (2f * Mathf.PI / 1024f);
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}

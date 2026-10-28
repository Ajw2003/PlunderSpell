using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// The one test of whether a guard can stand on a floor point: a floor that is flat enough, with the guard's
    /// capsule (lifted a step) clear of everything. The editor baker (<c>CastleNavTileBaker</c>) and the run-time
    /// refresh after a break (<see cref="CastleNavGraph.RefreshAfterBreak"/>) both use it, so a cell means the
    /// same thing whenever it was decided.
    /// </summary>
    public static class CastleNavProbe
    {
        public const float GuardRadius = 0.4f;
        public const float GuardHeight = 1.85f;
        public const float MinNormalY = 0.7f;
        public const float StepLift = CastleNavTile.StepHeight + 0.01f;

        /// <summary>
        /// True when something overlaps the standing capsule on <paramref name="floor"/>. <paramref name="scratch"/> needs
        /// room for one collider (baker) or a few dozen (<paramref name="castleOnly"/>). With <paramref name="castleOnly"/>
        /// (run time) only the built castle counts: players, guards and loot move, and must not take cells out of the map.
        /// </summary>
        public static bool IsBlocked(PhysicsScene physics, Vector3 floor, Collider[] scratch, bool castleOnly = false)
        {
            Vector3 bottom = floor + Vector3.up * (StepLift + GuardRadius);
            Vector3 top = floor + Vector3.up * (GuardHeight - GuardRadius);
            int count = physics.OverlapCapsule(bottom, top, GuardRadius, scratch, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (!castleOnly || IsCastle(scratch[i]))
                    return true;
            }
            return false;
        }

        /// <summary>True when the guard capsule cannot slide from one floor point to the next without touching the castle.</summary>
        public static bool SweepBlocked(Vector3 from, Vector3 to, RaycastHit[] scratch)
        {
            Vector3 along = to - from;
            float distance = along.magnitude;
            int count = Physics.CapsuleCastNonAlloc(from + Vector3.up * (StepLift + GuardRadius),
                from + Vector3.up * (GuardHeight - GuardRadius), GuardRadius, along / distance, scratch, distance, ~0,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                if (IsCastle(scratch[i].collider))
                    return true;
            }
            return false;
        }

        /// <summary>True when a floor surface, flat enough to stand on, lies within 0.1 m of <paramref name="floor"/>.</summary>
        public static bool HasFloor(PhysicsScene physics, Vector3 floor)
        {
            return physics.Raycast(floor + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.2f, ~0,
                QueryTriggerInteraction.Ignore) && hit.normal.y >= MinNormalY;
        }

        // Same test the Ophan uses for "castle stone": any collider under the generator, room or dressing.
        private static bool IsCastle(Collider collider) => collider.GetComponentInParent<ProceduralCastleGenerator>() != null;
    }
}

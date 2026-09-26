using System;
using System.Collections.Generic;
using RogueAi.Castle;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>Where one guard starts, and the route it walks.</summary>
    public readonly struct GuardPlacement
    {
        /// <summary>Index into <see cref="ProceduralCastleData.PlacedModules"/> the guard starts in.</summary>
        public readonly int ModuleIndex;

        /// <summary>World position the guard spawns at.</summary>
        public readonly Vector3 Position;

        /// <summary>Room centres the guard patrols between, in order. Never empty.</summary>
        public readonly IReadOnlyList<Vector3> PatrolRoute;

        /// <summary>The zone this guard is posted in.</summary>
        public readonly CastleZone Zone;

        public GuardPlacement(int moduleIndex, Vector3 position, IReadOnlyList<Vector3> patrolRoute,
            CastleZone zone)
        {
            ModuleIndex = moduleIndex;
            Position = position;
            PatrolRoute = patrolRoute;
            Zone = zone;
        }
    }

    /// <summary>
    /// Decides the garrison: how many guards, where they stand and what they walk, as a pure
    /// function of (layout, density, seed). Deterministic for the same reason the loot plan is —
    /// every peer derives the identical garrison from the replicated seed.
    ///
    /// Guards are posted where the treasure is. That pairing is the whole tension of the route in:
    /// the rooms worth entering are the rooms being watched.
    /// </summary>
    public static class GuardPlacementPlanner
    {
        /// <summary>Chance a room in each zone is guarded. Denser toward the centre, like the loot.</summary>
        public static float DensityFor(CastleZone zone, float scale = 1f)
        {
            float density;
            switch (zone)
            {
                case CastleZone.CurtainWall: density = 0.20f; break;
                case CastleZone.OuterBailey: density = 0.25f; break;
                case CastleZone.InnerWard: density = 0.35f; break;
                case CastleZone.Keep: density = 0.45f; break;
                case CastleZone.Crypt: density = 0.55f; break;
                default: density = 0f; break;
            }
            return Mathf.Clamp01(density * scale);
        }

        /// <summary>
        /// Rooms within this many grid cells of the arrival portal get no guard. The players step out
        /// of it, so a guard next door sees them on frame one and kills a player
        /// who is still reading the HUD. Two cells, and patrols stay out of it too: at one cell a
        /// guard next door still walked its route through the gate and killed a player standing at
        /// the spawn within about twenty seconds (seen in co-op testing, 2026-09-23).
        /// </summary>
        public const int SafeEntranceRadius = 2;

        /// <summary>
        /// Plans the garrison. The gatehouse is never guarded, and no guard is posted or patrols
        /// within <see cref="SafeEntranceRadius"/> cells of <paramref name="safeModuleIndex"/>: the
        /// arrival portal, which is also the way out, so the team is not shot while stepping out of
        /// it and every raid does not end in the same fight on the exit. Left at -1 the ring centres
        /// on the gatehouse, as it did before players arrived by portal.
        /// </summary>
        public static List<GuardPlacement> Plan(ProceduralCastleData castle, int seed,
            float densityScale = 1f, int safeModuleIndex = -1)
        {
            var placements = new List<GuardPlacement>();
            if (castle?.PlacedModules == null)
                return placements;

            // A third independent stream, so changing the garrison cannot shift the castle or the loot.
            var rng = new System.Random(unchecked(seed * 31 + 6151));

            int centreIndex = safeModuleIndex >= 0 ? safeModuleIndex : castle.ExtractionExitIndex;
            bool hasSafeCentre = centreIndex >= 0 && centreIndex < castle.PlacedModules.Count;
            Vector2Int safeCentre = hasSafeCentre ? castle.PlacedModules[centreIndex].GridPosition : default;

            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];

                if (module.IsExtractionExit || i == castle.ExtractionExitIndex)
                    continue;

                // Rolled before the safe-ring check so the rest of the garrison stays where it was.
                if (rng.NextDouble() > DensityFor(module.Zone, densityScale))
                    continue;

                if (hasSafeCentre && ChebyshevDistance(module.GridPosition, safeCentre) <= SafeEntranceRadius)
                    continue;

                placements.Add(new GuardPlacement(
                    i,
                    module.Position + Vector3.up * 0.1f,
                    BuildRoute(castle, i, rng, hasSafeCentre, safeCentre),
                    module.Zone));
            }

            return placements;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b) =>
            Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));

        /// <summary>
        /// A patrol route: this room plus up to two adjacent rooms. Adjacency (not any room) keeps
        /// the route walkable — the castle is 4-connected by construction, so neighbouring cells are
        /// always reachable from one another.
        /// </summary>
        private static List<Vector3> BuildRoute(ProceduralCastleData castle, int moduleIndex,
            System.Random rng, bool hasSafeCentre, Vector2Int safeCentre)
        {
            ProceduralCastleData.PlacedModule home = castle.PlacedModules[moduleIndex];
            var route = new List<Vector3> { home.Position };

            var neighbours = new List<Vector3>();
            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                if (i == moduleIndex)
                    continue;

                Vector2Int delta = castle.PlacedModules[i].GridPosition - home.GridPosition;
                if (Math.Abs(delta.x) + Math.Abs(delta.y) != 1)
                    continue;
                // A patrol never walks into the safe ring around the portal.
                if (hasSafeCentre && ChebyshevDistance(castle.PlacedModules[i].GridPosition, safeCentre) <= SafeEntranceRadius)
                    continue;
                neighbours.Add(castle.PlacedModules[i].Position);
            }

            int take = Mathf.Min(2, neighbours.Count);
            for (int i = 0; i < take; i++)
            {
                int pick = rng.Next(0, neighbours.Count);
                route.Add(neighbours[pick]);
                neighbours.RemoveAt(pick);
            }

            return route;
        }
    }
}

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
        /// Plans the garrison. The extraction room is deliberately left unguarded: a guard standing
        /// on the exit would turn every raid into the same fight, rather than a choice about when to
        /// leave.
        /// </summary>
        public static List<GuardPlacement> Plan(ProceduralCastleData castle, int seed, float densityScale = 1f)
        {
            var placements = new List<GuardPlacement>();
            if (castle?.PlacedModules == null)
                return placements;

            // A third independent stream, so changing the garrison cannot shift the castle or the loot.
            var rng = new System.Random(unchecked(seed * 31 + 6151));

            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];

                if (module.IsExtractionExit || i == castle.ExtractionExitIndex)
                    continue;

                if (rng.NextDouble() > DensityFor(module.Zone, densityScale))
                    continue;

                placements.Add(new GuardPlacement(
                    i,
                    module.Position + Vector3.up * 0.1f,
                    BuildRoute(castle, i, rng),
                    module.Zone));
            }

            return placements;
        }

        /// <summary>
        /// A patrol route: this room plus up to two adjacent rooms. Adjacency (not any room) keeps
        /// the route walkable — the castle is 4-connected by construction, so neighbouring cells are
        /// always reachable from one another.
        /// </summary>
        private static List<Vector3> BuildRoute(ProceduralCastleData castle, int moduleIndex,
            System.Random rng)
        {
            ProceduralCastleData.PlacedModule home = castle.PlacedModules[moduleIndex];
            var route = new List<Vector3> { home.Position };

            var neighbours = new List<Vector3>();
            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                if (i == moduleIndex)
                    continue;

                Vector2Int delta = castle.PlacedModules[i].GridPosition - home.GridPosition;
                if (Math.Abs(delta.x) + Math.Abs(delta.y) == 1)
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

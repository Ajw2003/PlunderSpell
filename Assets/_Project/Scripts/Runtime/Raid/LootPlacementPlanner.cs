using System.Collections.Generic;
using RogueAi.Castle;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>Where one piece of loot goes, and what it is.</summary>
    public readonly struct LootPlacement
    {
        /// <summary>Index into <see cref="ProceduralCastleData.PlacedModules"/>.</summary>
        public readonly int ModuleIndex;

        /// <summary>World position to spawn at.</summary>
        public readonly Vector3 Position;

        /// <summary>Which table entry to spawn.</summary>
        public readonly RaidLootTable.Entry Entry;

        /// <summary>The zone the loot sits in — deeper zones are worth more and cost more to reach.</summary>
        public readonly CastleZone Zone;

        public LootPlacement(int moduleIndex, Vector3 position, RaidLootTable.Entry entry, CastleZone zone)
        {
            ModuleIndex = moduleIndex;
            Position = position;
            Entry = entry;
            Zone = zone;
        }
    }

    /// <summary>
    /// Decides what loot goes where, as a pure function of (castle layout, loot table, seed).
    ///
    /// Purity is the point twice over. It is deterministic, so every peer plans the identical haul
    /// from the replicated seed and no loot manifest needs to go over the wire. And it touches no
    /// scene, so the rules below are assertable in a test rather than eyeballed in play:
    ///
    /// <list type="bullet">
    /// <item>Loot density rises toward the centre — the Crypt always has something, the curtain wall
    /// rarely does.</item>
    /// <item>The extraction room never holds loot. Free treasure at the exit would delete the carry,
    /// which is the whole game.</item>
    /// <item>The crypt final chamber always gets its richest eligible entry, so there is always a
    /// reason to go all the way in.</item>
    /// </list>
    /// </summary>
    public static class LootPlacementPlanner
    {
        /// <summary>Vertical offset so spawned loot rests above the floor plane rather than inside it.</summary>
        public const float SpawnHeight = 0.5f;

        /// <summary>Maximum horizontal scatter from a room's centre, in metres.</summary>
        public const float ScatterRadius = 3f;

        /// <summary>
        /// Plans the haul for a castle. Returns an empty list (never null) when the table is empty,
        /// which is the data-only case the generator itself already supports.
        /// </summary>
        public static List<LootPlacement> Plan(ProceduralCastleData castle, RaidLootTable table, int seed) =>
            Plan(castle, table, seed, null);

        /// <summary>
        /// Plans the haul, putting each piece on one of its room's authored loot anchors (a table
        /// top, a chest lid, an altar) when <paramref name="registry"/> knows the room's anchors,
        /// and never on a curtain-wall cell: those are the open strips between the rampart and the
        /// rooms, and loot there read as dropped rather than kept.
        /// </summary>
        public static List<LootPlacement> Plan(ProceduralCastleData castle, RaidLootTable table, int seed,
            CastleRoomRegistry registry)
        {
            var placements = new List<LootPlacement>();
            if (castle == null || castle.PlacedModules == null || table == null)
                return placements;

            // Deliberately a different stream from the castle's own RNG: re-rolling the loot table
            // must not be able to shift the castle layout.
            var rng = new System.Random(unchecked(seed * 31 + 977));

            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];

                // Never at the exit: the carry out is the game.
                if (module.IsExtractionExit || i == castle.ExtractionExitIndex)
                    continue;

                // Only in rooms (see the summary above).
                if (registry != null && module.Zone == CastleZone.CurtainWall)
                    continue;

                Vector3[] anchors = registry?.GetById(module.RoomId)?.LootAnchors;

                List<RaidLootTable.Entry> pool = table.EntriesFor(module.Zone);
                if (pool.Count == 0)
                    continue;

                bool isCryptFinal = module.IsCryptEntry || i == castle.CryptStartIndex;

                // The crypt final chamber is guaranteed, and guaranteed to be the best thing there.
                if (isCryptFinal)
                {
                    placements.Add(new LootPlacement(i, PlaceIn(module, anchors, rng, 0f),
                        RichestOf(pool), module.Zone));
                    continue;
                }

                if (rng.NextDouble() > table.DensityFor(module.Zone))
                    continue;

                placements.Add(new LootPlacement(i, PlaceIn(module, anchors, rng, ScatterRadius),
                    PickWeighted(pool, rng), module.Zone));
            }

            return placements;
        }

        /// <summary>Total coin value of a plan, if every piece were extracted intact.</summary>
        public static float TotalWorth(List<LootPlacement> placements)
        {
            float total = 0f;
            for (int i = 0; i < placements.Count; i++)
            {
                RaidLootTable.Entry entry = placements[i].Entry;
                if (entry?.Item != null)
                    total += entry.Item.Worth;
            }
            return total;
        }

        private static RaidLootTable.Entry RichestOf(List<RaidLootTable.Entry> pool)
        {
            RaidLootTable.Entry best = pool[0];
            for (int i = 1; i < pool.Count; i++)
            {
                if (pool[i].Item.Worth > best.Item.Worth)
                    best = pool[i];
            }
            return best;
        }

        private static RaidLootTable.Entry PickWeighted(List<RaidLootTable.Entry> pool, System.Random rng)
        {
            int total = 0;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(1, pool[i].Weight);

            int roll = rng.Next(0, total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(1, pool[i].Weight);
                if (roll < 0)
                    return pool[i];
            }
            return pool[pool.Count - 1];
        }

        /// <summary>Above an anchor lift loot only this far: a fragile piece dropped from the old
        /// 0.5 m spawn height onto a table could break on landing.</summary>
        public const float AnchorLift = 0.08f;

        private static Vector3 PlaceIn(ProceduralCastleData.PlacedModule module, Vector3[] anchors,
            System.Random rng, float scatterRadius)
        {
            if (anchors == null || anchors.Length == 0)
                return Scatter(module.Position, rng, scatterRadius);
            Vector3 anchor = anchors[rng.Next(0, anchors.Length)];
            return module.Position + module.Rotation * anchor + Vector3.up * AnchorLift;
        }

        private static Vector3 Scatter(Vector3 centre, System.Random rng, float radius)
        {
            if (radius <= 0f)
                return centre + Vector3.up * SpawnHeight;

            double angle = rng.NextDouble() * System.Math.PI * 2.0;
            double distance = rng.NextDouble() * radius;
            return new Vector3(
                centre.x + (float)(System.Math.Cos(angle) * distance),
                centre.y + SpawnHeight,
                centre.z + (float)(System.Math.Sin(angle) * distance));
        }
    }
}

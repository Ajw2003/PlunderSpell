using System.Collections.Generic;
using UnityEngine;

namespace RogueAi.Castle
{
    /// <summary>One piece of dressing placed in a castle.</summary>
    [System.Serializable]
    public struct PlacedDressing
    {
        public string Id;
        public DressingKind Kind;
        public Vector2Int Cell;
        public Vector3 Position;
        public Quaternion Rotation;

        public PlacedDressing(string id, DressingKind kind, Vector2Int cell, Vector3 position, Quaternion rotation)
        {
            Id = id;
            Kind = kind;
            Cell = cell;
            Position = position;
            Rotation = rotation;
        }
    }

    /// <summary>
    /// Decides the bailey's dressing from the layout and the seed (docs/plans/night-atmosphere.md,
    /// section 4), on its own random stream so it cannot shift the castle, the loot or the garrison:
    /// <list type="bullet">
    /// <item>every straight curtain-wall cell gets a curtain dressing, never the same one twice in a
    /// row round the ring;</item>
    /// <item>the straight cells either side of the gatehouse always get the gate yard;</item>
    /// <item>the gatehouse gets the sealed gate;</item>
    /// <item>every carved interior cell gets a courtyard of its zone.</item>
    /// </list>
    /// Pure, so every peer derives the same dressing and tests need no scene.
    /// </summary>
    public static class CastleDressingPlanner
    {
        /// <summary>
        /// Plans the dressing. <paramref name="straightWallIds"/> are the room ids the layout uses for
        /// a plain run of curtain wall; the other curtain pieces (corners, bastions) stay bare.
        /// </summary>
        public static List<PlacedDressing> Plan(ProceduralCastleData castle, int seed, CastleDressingSet set,
            ICollection<string> straightWallIds, int curtainWallRadius, float cellSize)
        {
            var placed = new List<PlacedDressing>();
            if (castle?.PlacedModules == null || set == null)
                return placed;

            var rng = new System.Random(unchecked(seed * 41 + 3571));
            var curtainPool = new List<CastleDressingSet.Entry>();
            CastleDressingSet.Entry gateYard = null, gateSealed = null;
            foreach (CastleDressingSet.Entry entry in set.Entries)
            {
                if (entry == null)
                    continue;
                if (entry.Kind == DressingKind.Curtain)
                    curtainPool.Add(entry);
                else if (entry.Kind == DressingKind.GateYard)
                    gateYard ??= entry;
                else if (entry.Kind == DressingKind.GateSealed)
                    gateSealed ??= entry;
            }

            Vector2Int gateCell = castle.ExtractionExitIndex >= 0 && castle.ExtractionExitIndex < castle.PlacedModules.Count
                ? castle.PlacedModules[castle.ExtractionExitIndex].GridPosition
                : new Vector2Int(int.MinValue, int.MinValue);

            if (gateSealed != null && castle.ExtractionExitIndex >= 0)
            {
                ProceduralCastleData.PlacedModule gate = castle.PlacedModules[castle.ExtractionExitIndex];
                placed.Add(new PlacedDressing(gateSealed.Id, DressingKind.GateSealed, gate.GridPosition, gate.Position, gate.Rotation));
            }

            // Walk the straight runs round the ring in order, so "twice in a row" means neighbours.
            var straight = new List<ProceduralCastleData.PlacedModule>();
            foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
            {
                if (module.Zone == CastleZone.CurtainWall && straightWallIds.Contains(module.RoomId)
                    && Chebyshev(module.GridPosition) == curtainWallRadius)
                    straight.Add(module);
            }
            straight.Sort((a, b) => RingAngle(a.GridPosition).CompareTo(RingAngle(b.GridPosition)));

            string previous = null;
            foreach (ProceduralCastleData.PlacedModule module in straight)
            {
                CastleDressingSet.Entry chosen;
                if (gateYard != null && IsBesideAlongRing(module.GridPosition, gateCell))
                    chosen = gateYard;
                else
                    chosen = PickWeighted(curtainPool, rng, previous);
                if (chosen == null)
                    continue;
                placed.Add(new PlacedDressing(chosen.Id, chosen.Kind, module.GridPosition, module.Position, module.Rotation));
                previous = chosen.Id;
            }

            // Carved interior cells: whatever the ring's interior leaves empty.
            var occupied = new HashSet<Vector2Int>();
            foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
                occupied.Add(module.GridPosition);
            int limit = curtainWallRadius - 1;
            for (int x = -limit; x <= limit; x++)
            {
                for (int y = -limit; y <= limit; y++)
                {
                    var cell = new Vector2Int(x, y);
                    if (occupied.Contains(cell))
                        continue;
                    CastleZone zone = ZoneForRing(Chebyshev(cell), curtainWallRadius);
                    var pool = new List<CastleDressingSet.Entry>();
                    foreach (CastleDressingSet.Entry entry in set.Entries)
                    {
                        if (entry != null && entry.Kind == DressingKind.Courtyard && entry.Zone == zone)
                            pool.Add(entry);
                    }
                    CastleDressingSet.Entry chosen = PickWeighted(pool, rng, null);
                    if (chosen == null)
                        continue;
                    Quaternion turn = Quaternion.Euler(0f, 90f * rng.Next(4), 0f);
                    placed.Add(new PlacedDressing(chosen.Id, DressingKind.Courtyard, cell,
                        new Vector3(cell.x * cellSize, 0f, cell.y * cellSize), turn));
                }
            }

            return placed;
        }

        /// <summary>The zone a ring of the interior belongs to, matching the generator's zoning.</summary>
        public static CastleZone ZoneForRing(int ring, int curtainWallRadius)
        {
            if (ring == 0)
                return CastleZone.Crypt;
            if (ring >= curtainWallRadius - 1)
                return CastleZone.OuterBailey;
            if (ring == curtainWallRadius - 2)
                return CastleZone.InnerWard;
            return CastleZone.Keep;
        }

        private static CastleDressingSet.Entry PickWeighted(List<CastleDressingSet.Entry> pool, System.Random rng, string avoid)
        {
            int total = 0;
            foreach (CastleDressingSet.Entry entry in pool)
            {
                if (entry.Id != avoid)
                    total += Mathf.Max(0, entry.Weight);
            }
            if (total <= 0)
                return pool.Count > 0 ? pool[0] : null;

            int roll = rng.Next(total);
            foreach (CastleDressingSet.Entry entry in pool)
            {
                if (entry.Id == avoid)
                    continue;
                roll -= Mathf.Max(0, entry.Weight);
                if (roll < 0)
                    return entry;
            }
            return null;
        }

        private static int Chebyshev(Vector2Int c) => Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.y));

        /// <summary>Two ring cells are neighbours along the ring when they touch on a side.</summary>
        private static bool IsBesideAlongRing(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        private static float RingAngle(Vector2Int c) => Mathf.Atan2(c.y, c.x);
    }
}

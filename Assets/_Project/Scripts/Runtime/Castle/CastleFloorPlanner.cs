using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Decides the stacked castle's rooms (#247, docs/plans/multi-floor-castle.md section 1): a 5 × 5 ground floor
    /// (bailey ring, inner-ward ring, a down-stair at the centre), a 3 × 3 keep above, a 3 × 3 crypt below.
    /// Pure and seeded, so every peer plans the same castle. Courtyards come only from the bailey ring.
    /// Order is fixed (crypt, ground, keep; inner ring first; then x, then y) so placement indices depend on
    /// the seed alone.
    /// </summary>
    public static class CastleFloorPlanner
    {
        public const int CurtainWallRadius = 3;

        public static readonly Vector2Int[] UpStairCells = { new Vector2Int(-1, 0), new Vector2Int(1, 0) };

        public static List<PlannedRoom> Plan(System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            var plan = new List<PlannedRoom>();
            PlanCrypt(plan, rng);
            PlanGround(plan, rng, fillFraction, gateApproach);
            PlanKeep(plan);
            return plan;
        }

        private static void PlanCrypt(List<PlannedRoom> plan, System.Random rng)
        {
            List<Vector2Int> ring = Ring(1);
            Vector2Int final = ring[rng.Next(0, ring.Count)];
            plan.Add(new PlannedRoom(Vector2Int.zero, CastleLevels.Crypt, CastleZone.InnerWard, PlannedRoomKind.StairDown, Vector2Int.up));
            foreach (Vector2Int cell in ring)
            {
                PlannedRoomKind kind = cell == final ? PlannedRoomKind.FinalChamber : PlannedRoomKind.Room;
                plan.Add(new PlannedRoom(cell, CastleLevels.Crypt, CastleZone.Crypt, kind, Vector2Int.zero));
            }
        }

        private static void PlanGround(List<PlannedRoom> plan, System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            foreach (Vector2Int cell in Ring(1))
            {
                bool up = System.Array.IndexOf(UpStairCells, cell) >= 0;
                plan.Add(new PlannedRoom(cell, CastleLevels.Ground, CastleZone.InnerWard,
                    up ? PlannedRoomKind.StairUp : PlannedRoomKind.Room, up ? -cell : Vector2Int.zero));
            }
            foreach (Vector2Int cell in KeptBailey(rng, fillFraction, gateApproach))
                plan.Add(new PlannedRoom(cell, CastleLevels.Ground, CastleZone.OuterBailey, PlannedRoomKind.Room, Vector2Int.zero));
        }

        // The stair heads at (±1, 0) belong to the up-stair modules below them.
        private static void PlanKeep(List<PlannedRoom> plan)
        {
            var cells = new List<Vector2Int> { Vector2Int.zero };
            cells.AddRange(Ring(1));
            foreach (Vector2Int cell in cells)
            {
                if (System.Array.IndexOf(UpStairCells, cell) < 0)
                    plan.Add(new PlannedRoom(cell, CastleLevels.Keep, CastleZone.Keep, PlannedRoomKind.Room, Vector2Int.zero));
            }
        }

        // Carves courtyards from the bailey ring where the ground stays one 4-connected region.
        private static List<Vector2Int> KeptBailey(System.Random rng, float fillFraction, Vector2Int gateApproach)
        {
            List<Vector2Int> bailey = Ring(2);
            var ground = new HashSet<Vector2Int>(bailey) { Vector2Int.zero };
            foreach (Vector2Int cell in Ring(1))
                ground.Add(cell);
            var order = new List<Vector2Int>(bailey);
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
            int target = Mathf.RoundToInt(bailey.Count * (1f - fillFraction));
            int carved = 0;
            for (int i = 0; i < order.Count && carved < target; i++)
            {
                if (order[i] == gateApproach)
                    continue;
                ground.Remove(order[i]);
                if (IsConnected(ground))
                    carved++;
                else
                    ground.Add(order[i]);
            }
            var kept = new List<Vector2Int>();
            foreach (Vector2Int cell in bailey)
            {
                if (ground.Contains(cell))
                    kept.Add(cell);
            }
            return kept;
        }

        /// <summary>A ring's cells in a fixed order: x, then y.</summary>
        private static List<Vector2Int> Ring(int radius)
        {
            var cells = new List<Vector2Int>();
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(y)) == radius)
                        cells.Add(new Vector2Int(x, y));
            return cells;
        }

        private static bool IsConnected(HashSet<Vector2Int> cells)
        {
            var seen = new HashSet<Vector2Int> { Vector2Int.zero };
            var frontier = new Queue<Vector2Int>(seen);
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };
            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
                foreach (Vector2Int dir in dirs)
                {
                    if (cells.Contains(current + dir) && seen.Add(current + dir))
                        frontier.Enqueue(current + dir);
                }
            }
            return seen.Count == cells.Count;
        }
    }
}

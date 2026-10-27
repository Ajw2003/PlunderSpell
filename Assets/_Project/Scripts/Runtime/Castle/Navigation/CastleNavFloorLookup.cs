using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// The floor under a point: the horizontally nearest walkable column wins, then the layer nearest in
    /// height inside it. A 3D-nearest pick let a guard on a ramp, 0.2 m short of the gallery edge, match
    /// the lower ramp cell beside it and settle there (#255). Floors more than <c>maxRise</c> above or
    /// below are not this point's floor, so a gallery over a room is not picked from the room.
    /// </summary>
    public static class CastleNavFloorLookup
    {
        private const float SameColumnEpsilon = 0.0001f;

        /// <summary>The floor cell under <paramref name="position"/>, or <see cref="CastleNavGrid.NoCell"/>.</summary>
        public static int Find(CastleNavGrid grid, Vector3 position, float maxDistance)
        {
            int reach = Mathf.FloorToInt(maxDistance / CastleNavTile.CellSize + 0.5f);
            int centreColumn = grid.CastleColumn(position.x), centreRow = grid.CastleRow(position.z);
            int best = CastleNavGrid.NoCell;
            float bestFlat = float.MaxValue, bestRise = float.MaxValue;
            for (int dr = -reach; dr <= reach; dr++)
            {
                for (int dc = -reach; dc <= reach; dc++)
                {
                    int column = centreColumn + dc, row = centreRow + dr;
                    for (int level = CastleLevels.Lowest; level < CastleLevels.Lowest + CastleLevels.Count; level++)
                    {
                        for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                            Consider(grid, position, maxDistance, grid.CellAt(column, row, layer, level), ref best, ref bestFlat, ref bestRise);
                    }
                }
            }
            return best;
        }

        private static void Consider(CastleNavGrid grid, Vector3 position, float maxDistance, int cell,
            ref int best, ref float bestFlat, ref float bestRise)
        {
            if (cell < 0 || !grid.IsWalkable(cell))
                return;
            Vector3 at = grid.CellPosition(cell);
            float flat = (at.x - position.x) * (at.x - position.x) + (at.z - position.z) * (at.z - position.z);
            float rise = Mathf.Abs(at.y - position.y);
            if (flat + rise * rise > maxDistance * maxDistance)
                return;
            bool nearerColumn = flat < bestFlat - SameColumnEpsilon;
            bool sameColumnNearerFloor = Mathf.Abs(flat - bestFlat) <= SameColumnEpsilon && rise < bestRise;
            if (!nearerColumn && !sameColumnNearerFloor)
                return;
            best = cell;
            bestFlat = flat;
            bestRise = rise;
        }
    }
}

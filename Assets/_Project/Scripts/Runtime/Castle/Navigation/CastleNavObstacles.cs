using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Takes the bailey's dressing (carts, woodpiles, crates) out of the walk map. Room tiles are baked
    /// from the bare module prefabs, and the dressing is placed per raid on top of them, so without this
    /// a route runs straight through a cart and the guard's sweep stops it there for good. The old NavMesh
    /// was baked after the dressing, so this restores what it gave.
    ///
    /// Only cells under a dressing piece's bounds (grown by the guard's radius) are tested, each with the
    /// same standing-capsule check the tile baker uses. The bare room already passed that check at every
    /// walkable cell, so anything the capsule hits now is the dressing.
    /// </summary>
    public static class CastleNavObstacles
    {
        // Same guard capsule and step lift as CastleNavTileBaker, so a stamped cell means what a baked one does.
        private const float GuardRadius = 0.4f;
        private const float GuardHeight = 1.85f;
        private const float StepLift = CastleNavTile.StepHeight + 0.01f;

        private static readonly List<Collider> s_colliders = new List<Collider>();

        /// <summary>Clears every walkable cell where a standing guard would overlap one of <paramref name="obstacles"/>. Returns how many.</summary>
        public static int Stamp(CastleNavGrid grid, IReadOnlyList<GameObject> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
                return 0;

            // Instantiated this frame; the physics scene has not seen them move into place yet.
            Physics.SyncTransforms();
            int cleared = 0;
            for (int i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i] != null && TryGetSolidBounds(obstacles[i], out Bounds bounds))
                    cleared += ClearCellsUnder(grid, bounds);
            }
            return cleared;
        }

        private static bool TryGetSolidBounds(GameObject obstacle, out Bounds bounds)
        {
            bounds = default;
            bool any = false;
            obstacle.GetComponentsInChildren(s_colliders);
            for (int i = 0; i < s_colliders.Count; i++)
            {
                Collider collider = s_colliders[i];
                if (collider.isTrigger || !collider.enabled)
                    continue;
                if (any)
                    bounds.Encapsulate(collider.bounds);
                else
                    bounds = collider.bounds;
                any = true;
            }
            return any;
        }

        private static int ClearCellsUnder(CastleNavGrid grid, Bounds bounds)
        {
            bounds.Expand(new Vector3(GuardRadius * 2f, 0f, GuardRadius * 2f));
            int cleared = 0;
            int firstColumn = grid.CastleColumn(bounds.min.x), lastColumn = grid.CastleColumn(bounds.max.x);
            int firstRow = grid.CastleRow(bounds.min.z), lastRow = grid.CastleRow(bounds.max.z);
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                    cleared += ClearColumnIfBlocked(grid, column, row, bounds);
            }
            return cleared;
        }

        private static int ClearColumnIfBlocked(CastleNavGrid grid, int castleColumn, int castleRow, Bounds bounds)
        {
            int cleared = 0;
            for (int layer = 0; layer < CastleNavTile.Layers; layer++)
            {
                int cell = grid.CellAt(castleColumn, castleRow, layer);
                if (cell == CastleNavGrid.NoCell || !grid.IsWalkable(cell))
                    continue;
                Vector3 floor = grid.CellPosition(cell);
                // A dressing piece wholly above or below this floor (a gallery over a cart) cannot block it.
                if (floor.y + GuardHeight < bounds.min.y || floor.y > bounds.max.y)
                    continue;
                if (IsBlocked(floor))
                {
                    grid.ClearWalkable(cell);
                    cleared++;
                }
            }
            return cleared;
        }

        private static bool IsBlocked(Vector3 floor)
        {
            Vector3 bottom = floor + Vector3.up * (StepLift + GuardRadius);
            Vector3 top = floor + Vector3.up * (GuardHeight - GuardRadius);
            return Physics.CheckCapsule(bottom, top, GuardRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }
    }
}

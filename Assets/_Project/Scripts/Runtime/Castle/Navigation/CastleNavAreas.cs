using System;
using System.Collections.Generic;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Connected walkable areas. Each room's cells are flood-labelled first; archways then join the
    /// labels of the rooms they open between (union-find). Two cells are reachable exactly when they
    /// end up with the same area id, which is what makes "is it reachable" a single comparison.
    /// </summary>
    public sealed class CastleNavAreas
    {
        private readonly int[] _parent;

        private CastleNavAreas(int[] parent)
        {
            _parent = parent;
        }

        /// <summary>Labels each room's walkable cells by what connects inside that room alone.</summary>
        public static CastleNavAreas Label(CastleNavGrid grid)
        {
            var parent = new List<int>();
            var pending = new int[CastleNavGrid.CellsPerModule];
            for (int module = 0; module < grid.ModuleCount; module++)
            {
                for (int local = 0; local < CastleNavGrid.CellsPerModule; local++)
                {
                    int cell = module * CastleNavGrid.CellsPerModule + local;
                    if (!grid.IsWalkable(cell) || grid.AreaId(cell) >= 0)
                        continue;
                    int label = parent.Count;
                    parent.Add(label);
                    FloodRoom(grid, module, local, label, pending);
                }
            }
            return new CastleNavAreas(parent.ToArray());
        }

        private static void FloodRoom(CastleNavGrid grid, int module, int startLocal, int label, int[] pending)
        {
            int baseCell = module * CastleNavGrid.CellsPerModule;
            int count = 0;
            pending[count++] = startLocal;
            grid.SetAreaId(baseCell + startLocal, label);
            while (count > 0)
            {
                int local = pending[--count];
                for (int direction = 0; direction < 4; direction++)
                {
                    for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                    {
                        if (!grid.TryStep(module, local, direction, layer, out int next) ||
                            grid.AreaId(baseCell + next) >= 0)
                            continue;
                        grid.SetAreaId(baseCell + next, label);
                        pending[count++] = next;
                    }
                }
            }
        }

        /// <summary>Merges the areas of two cells, as when an archway joins them.</summary>
        public void Join(CastleNavGrid grid, int cellA, int cellB)
        {
            int rootA = Find(grid.AreaId(cellA)), rootB = Find(grid.AreaId(cellB));
            // The lower id always wins, so ids do not depend on the order links were found in.
            if (rootA != rootB)
                _parent[Math.Max(rootA, rootB)] = Math.Min(rootA, rootB);
        }

        /// <summary>Rewrites every cell's area id to its final root, after all joins.</summary>
        public void Flatten(CastleNavGrid grid)
        {
            int cells = grid.ModuleCount * CastleNavGrid.CellsPerModule;
            for (int cell = 0; cell < cells; cell++)
            {
                if (grid.AreaId(cell) >= 0)
                    grid.SetAreaId(cell, Find(grid.AreaId(cell)));
            }
        }

        private int Find(int id)
        {
            while (_parent[id] != id)
            {
                _parent[id] = _parent[_parent[id]];
                id = _parent[id];
            }
            return id;
        }
    }
}

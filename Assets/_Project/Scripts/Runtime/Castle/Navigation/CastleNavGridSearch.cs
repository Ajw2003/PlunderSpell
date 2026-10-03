using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// A* (or a full flood) over the cells of one room. Searches never leave their module: crossing
    /// between rooms is the portal search's job. Three named buffers let a path query keep the
    /// start room's and the goal room's costs alive while it walks the individual hops.
    /// </summary>
    public sealed class CastleNavGridSearch
    {
        /// <summary>Costs from the query's start cell to every cell of its room.</summary>
        public readonly NavSearchBuffer StartFlood = new NavSearchBuffer();
        /// <summary>Costs from the query's goal cell to every cell of its room.</summary>
        public readonly NavSearchBuffer GoalFlood = new NavSearchBuffer();
        /// <summary>Everything else: one hop across a room, or one archway's costs while building.</summary>
        public readonly NavSearchBuffer Hop = new NavSearchBuffer();

        private readonly CastleNavGrid _grid;
        private readonly NavMinHeap _heap;

        public CastleNavGridSearch(CastleNavGrid grid, NavMinHeap heap)
        {
            _grid = grid;
            _heap = heap;
        }

        /// <summary>
        /// Searches inside one module from <paramref name="startLocal"/> to <paramref name="goalLocal"/>
        /// (room-local cells), or with goal -1 floods the whole room. Leaves costs and parents in
        /// <paramref name="buffer"/>. Returns the cost to the goal, or infinity if there is no way.
        /// </summary>
        public float Run(NavSearchBuffer buffer, int module, int startLocal, int goalLocal)
        {
            buffer.Begin();
            _heap.Clear();
            buffer.Reach(startLocal, 0f, NavSearchBuffer.NoParent);
            _heap.Push(0f, startLocal);
            while (_heap.Count > 0)
            {
                int local = _heap.Pop();
                if (!buffer.TryClose(local))
                    continue;
                if (local == goalLocal)
                    return buffer.CostTo(local);
                Expand(buffer, module, local, goalLocal);
            }
            return goalLocal < 0 ? 0f : float.PositiveInfinity;
        }

        private void Expand(NavSearchBuffer buffer, int module, int local, int goalLocal)
        {
            int baseCell = module * CastleNavGrid.CellsPerModule;
            for (int direction = 0; direction < 4; direction++)
            {
                for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                {
                    if (!_grid.TryStep(module, local, direction, layer, out int next) || buffer.IsClosed(next))
                        continue;
                    float cost = buffer.CostTo(local) + _grid.StepCost(baseCell + local, baseCell + next);
                    if (cost >= buffer.CostTo(next))
                        continue;
                    buffer.Reach(next, cost, local);
                    _heap.Push(cost + Heuristic(next, goalLocal), next);
                }
            }
        }

        // Manhattan distance never overestimates on a 4-connected grid whose steps cost at least one cell.
        private static float Heuristic(int local, int goalLocal)
        {
            if (goalLocal < 0)
                return 0f;
            int columns = Mathf.Abs(local % CastleNavGrid.TileSize - goalLocal % CastleNavGrid.TileSize);
            int rows = Mathf.Abs(local / CastleNavGrid.TileSize % CastleNavGrid.TileSize
                                 - goalLocal / CastleNavGrid.TileSize % CastleNavGrid.TileSize);
            return (columns + rows) * CastleNavTile.CellSize;
        }

        /// <summary>
        /// Appends the cells of the path the last search in <paramref name="buffer"/> found, start
        /// first. When a hop begins on the cell the previous hop ended on, that point is not repeated.
        /// </summary>
        public void AppendPath(NavSearchBuffer buffer, int module, int goalLocal, List<Vector3> path)
        {
            int length = 0;
            for (int local = goalLocal; local >= 0; local = buffer.ParentOf(local))
                length++;
            int first = path.Count;
            for (int i = 0; i < length; i++)
                path.Add(default);
            int at = first + length - 1;
            for (int local = goalLocal; local >= 0; local = buffer.ParentOf(local))
                path[at--] = _grid.CellPosition(module * CastleNavGrid.CellsPerModule + local);
            if (first > 0 && length > 0 && path[first] == path[first - 1])
                path.RemoveAt(first);
        }
    }
}

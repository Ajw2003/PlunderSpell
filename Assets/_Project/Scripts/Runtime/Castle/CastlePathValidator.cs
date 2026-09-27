using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Validates that a generated castle is traversable from the crypt final chamber to the
    /// extraction exit. Runs once at generation time (never per-frame for enemies).
    ///
    /// The validator rasterises every placed module into a walkable boolean grid — each module
    /// covers an N×N footprint scaled by <see cref="CellScale"/> so adjacent modules' footprints
    /// touch — then runs a 4-directional A* with a Manhattan heuristic between the crypt and
    /// extraction cells.
    /// </summary>
    public static class CastlePathValidator
    {
        /// <summary>
        /// Resolution multiplier: each grid cell in the layout becomes a CellScale×CellScale block
        /// of walkable cells on the validation grid. Two layout cells one step apart therefore
        /// produce edge-adjacent blocks, preserving 4-connectivity for A*.
        /// </summary>
        public const int CellScale = 2;

        /// <summary>
        /// Attempts to find a walkable path from the crypt start to the extraction exit.
        /// </summary>
        /// <param name="data">The generated layout.</param>
        /// <param name="path">The resulting cell path on the validation grid (empty on failure).</param>
        /// <returns>True if a path exists; false otherwise.</returns>
        public static bool ValidatePath(ProceduralCastleData data, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();

            if (data == null || data.PlacedModules == null || data.PlacedModules.Count == 0)
                return false;

            // Locate crypt start and extraction end modules.
            int startIdx = data.CryptStartIndex;
            int endIdx = data.ExtractionExitIndex;

            if (startIdx < 0)
                startIdx = FindFirst(data, m => m.IsCryptEntry || m.Zone == CastleZone.Crypt);
            if (endIdx < 0)
                endIdx = FindFirst(data, m => m.IsExtractionExit || m.Zone == CastleZone.OuterBailey);

            if (startIdx < 0 || endIdx < 0)
                return false;

            // Compute grid bounds over all module cells.
            Vector2Int min = data.PlacedModules[0].GridPosition;
            Vector2Int max = min;
            foreach (var pm in data.PlacedModules)
            {
                min = Vector2Int.Min(min, pm.GridPosition);
                max = Vector2Int.Max(max, pm.GridPosition);
            }

            int width = (max.x - min.x + 1) * CellScale;
            int height = (max.y - min.y + 1) * CellScale;
            if (width <= 0 || height <= 0)
                return false;

            var grid = new bool[width, height];

            // Paint each module's CellScale×CellScale footprint.
            foreach (var pm in data.PlacedModules)
            {
                int baseX = (pm.GridPosition.x - min.x) * CellScale;
                int baseY = (pm.GridPosition.y - min.y) * CellScale;
                for (int dx = 0; dx < CellScale; dx++)
                    for (int dy = 0; dy < CellScale; dy++)
                        grid[baseX + dx, baseY + dy] = true;
            }

            Vector2Int start = CellCenter(data.PlacedModules[startIdx].GridPosition, min);
            Vector2Int end = CellCenter(data.PlacedModules[endIdx].GridPosition, min);

            List<Vector2Int> result = RunAstar(grid, start, end);
            if (result == null || result.Count == 0)
                return false;

            path = result;
            return true;
        }

        private static Vector2Int CellCenter(Vector2Int cell, Vector2Int min)
        {
            return new Vector2Int((cell.x - min.x) * CellScale, (cell.y - min.y) * CellScale);
        }

        private static int FindFirst(ProceduralCastleData data,
            System.Predicate<ProceduralCastleData.PlacedModule> pred)
        {
            for (int i = 0; i < data.PlacedModules.Count; i++)
                if (pred(data.PlacedModules[i]))
                    return i;
            return -1;
        }

        // --- A* -----------------------------------------------------------------

        /// <summary>A single search node with the usual f = g + h costs.</summary>
        private class Node
        {
            public Vector2Int Pos;
            public int G;   // cost from start
            public int H;   // heuristic to end
            public int F => G + H;
            public Node Parent;
        }

        private static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        /// <summary>
        /// Standard 4-directional A* with a Manhattan heuristic. Returns the path from
        /// <paramref name="start"/> to <paramref name="end"/> inclusive, or null if unreachable.
        /// </summary>
        internal static List<Vector2Int> RunAstar(bool[,] grid, Vector2Int start, Vector2Int end)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);

            if (!InBounds(start, width, height) || !InBounds(end, width, height))
                return null;
            if (!grid[start.x, start.y] || !grid[end.x, end.y])
                return null;

            var open = new List<Node>();
            var openLookup = new Dictionary<Vector2Int, Node>();
            var closed = new HashSet<Vector2Int>();

            var startNode = new Node { Pos = start, G = 0, H = Manhattan(start, end) };
            open.Add(startNode);
            openLookup[start] = startNode;

            while (open.Count > 0)
            {
                // Pop the lowest-F node (linear scan; grids here are small).
                int best = 0;
                for (int i = 1; i < open.Count; i++)
                    if (open[i].F < open[best].F || (open[i].F == open[best].F && open[i].H < open[best].H))
                        best = i;

                Node current = open[best];
                open.RemoveAt(best);
                openLookup.Remove(current.Pos);

                if (current.Pos == end)
                    return Reconstruct(current);

                closed.Add(current.Pos);

                foreach (Vector2Int d in Neighbours)
                {
                    Vector2Int np = current.Pos + d;
                    if (!InBounds(np, width, height) || !grid[np.x, np.y] || closed.Contains(np))
                        continue;

                    int tentativeG = current.G + 1;
                    if (openLookup.TryGetValue(np, out Node existing))
                    {
                        if (tentativeG < existing.G)
                        {
                            existing.G = tentativeG;
                            existing.Parent = current;
                        }
                    }
                    else
                    {
                        var neighbour = new Node
                        {
                            Pos = np,
                            G = tentativeG,
                            H = Manhattan(np, end),
                            Parent = current
                        };
                        open.Add(neighbour);
                        openLookup[np] = neighbour;
                    }
                }
            }

            return null; // no path
        }

        private static List<Vector2Int> Reconstruct(Node end)
        {
            var path = new List<Vector2Int>();
            Node n = end;
            while (n != null)
            {
                path.Add(n.Pos);
                n = n.Parent;
            }
            path.Reverse();
            return path;
        }

        private static bool InBounds(Vector2Int p, int w, int h)
            => p.x >= 0 && p.y >= 0 && p.x < w && p.y < h;

        private static int Manhattan(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}

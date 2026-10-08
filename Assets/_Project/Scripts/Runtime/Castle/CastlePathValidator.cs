using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Validates that a generated castle is traversable from the crypt final chamber to the
    /// extraction exit. Runs once at generation time (never per-frame for enemies).
    /// </summary>
    public static class CastlePathValidator
    {
        private static readonly Vector2Int[] Directions =
            { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        /// <summary>
        /// Whether the final chamber can be walked to the gate, floor by floor (#247): a breadth-first search over
        /// (cell, level). Rooms meet through archways where both open (<see cref="CastleStairRule"/>); a stair joins
        /// its own two levels; a room meets the curtain wall only at the gate or an entrance, on the ground.
        /// </summary>
        public static bool ValidatePath(ProceduralCastleData data, out List<Vector2Int> path)
        {
            path = new List<Vector2Int>();
            if (data?.PlacedModules == null || data.PlacedModules.Count == 0 ||
                data.CryptStartIndex < 0 || data.ExtractionExitIndex < 0)
                return false;

            var at = new Dictionary<(Vector2Int, int), int>();
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule m = data.PlacedModules[i];
                for (int level = m.Level; level <= m.TopLevel; level++)
                    at[(m.GridPosition, level)] = i;
            }

            ProceduralCastleData.PlacedModule start = data.PlacedModules[data.CryptStartIndex];
            var from = new Dictionary<(Vector2Int, int), (Vector2Int, int)>();
            var queue = new Queue<(Vector2Int, int)>();
            var first = (start.GridPosition, start.Level);
            if (!at.ContainsKey(first))
                return false;
            from[first] = first;
            queue.Enqueue(first);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                ProceduralCastleData.PlacedModule here = data.PlacedModules[at[node]];
                if (at[node] == data.ExtractionExitIndex)
                {
                    for (var n = node; ; n = from[n])
                    {
                        path.Insert(0, n.Item1);
                        if (n.Equals(first))
                            break;
                    }
                    return true;
                }
                foreach (var next in Neighbours(data, at, node, here))
                {
                    if (from.ContainsKey(next))
                        continue;
                    from[next] = node;
                    queue.Enqueue(next);
                }
            }
            return false;
        }

        private static IEnumerable<(Vector2Int, int)> Neighbours(ProceduralCastleData data,
            Dictionary<(Vector2Int, int), int> at, (Vector2Int, int) node, ProceduralCastleData.PlacedModule here)
        {
            (Vector2Int cell, int level) = node;
            for (int other = here.Level; other <= here.TopLevel; other++)          // a stair joins its own storeys
                if (other != level)
                    yield return (cell, other);
            foreach (Vector2Int dir in Directions)
            {
                var next = (cell + dir, level);
                if (!at.TryGetValue(next, out int index))
                    continue;
                ProceduralCastleData.PlacedModule there = data.PlacedModules[index];
                if (CastleNavArchwayRule.IsOpen(data, here, there, level))
                    yield return next;
            }
        }
    }
}

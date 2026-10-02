using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Plans smoothed routes and remembers them. Many guards sent at one spot from one room (the hue
    /// and cry) all want the same route, so a finished route is kept under its start cell and goal cell
    /// and handed out again. The key is the cell pair rather than the room pair: a route is only right
    /// from the exact cell it began at, and two guards in one room stand in different cells.
    ///
    /// Planning allocates (a cached route is a new array); that happens once per distinct request, never
    /// per frame. Cached routes are shared and never edited, so any number of guards can walk one.
    /// </summary>
    public sealed class GuardPathPlanner
    {
        private readonly Dictionary<long, Vector3[]> _cache = new Dictionary<long, Vector3[]>();
        private readonly List<Vector3> _cellChain = new List<Vector3>();
        private readonly GuardPathSmoother _smoother = new GuardPathSmoother();

        /// <summary>Routes served from the cache since creation. Surfaced for tests.</summary>
        public int CacheHits { get; private set; }

        /// <summary>Routes planned from scratch since creation. Surfaced for tests.</summary>
        public int CacheMisses { get; private set; }

        /// <summary>Forgets every route. Called when a door changes, since a route may now be wrong.</summary>
        public void Clear() => _cache.Clear();

        public bool TryPlan(IGuardNavigationMap map, Vector3 from, Vector3 to, float halfWidth,
            out Vector3[] route, out BlockedReason failure)
        {
            failure = BlockedReason.NoWalkableCell;
            route = null;
            int startCell = map.FindCell(from), goalCell = map.FindCell(to);
            if (startCell < 0 || goalCell < 0)
                return false;

            long key = ((long)startCell << 32) | (uint)goalCell;
            if (_cache.TryGetValue(key, out route))
            {
                CacheHits++;
                return true;
            }

            if (!map.TryFindPath(from, to, _cellChain, out failure))
                return false;
            CacheMisses++;
            _smoother.Smooth(_cellChain, map, halfWidth);
            route = _smoother.Kept.ToArray();
            _cache[key] = route;
            return true;
        }

        /// <summary>Walking length of a route, in metres.</summary>
        public static float LengthOf(Vector3[] route)
        {
            float length = 0f;
            for (int i = 1; i < route.Length; i++)
                length += Vector3.Distance(route[i - 1], route[i]);
            return length;
        }
    }
}

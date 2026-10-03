using System.Collections.Generic;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// A flat, empty floor for testing the navigation service without a castle: every point is
    /// walkable, a path is a straight line, and the floor is at height zero. Walls and players in a
    /// test come from real colliders, so the sweep is what is under test.
    /// </summary>
    public sealed class FlatNavigationMap : IGuardNavigationMap
    {
        private const float WaypointSpacing = 0.5f;

        public int Version => 0;

        public int FindCell(Vector3 position)
        {
            return Mathf.RoundToInt(position.x * 2f) * 4096 + Mathf.RoundToInt(position.z * 2f) + 1000000;
        }

        public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure)
        {
            failure = BlockedReason.Unreachable;
            path.Clear();
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(from, to) / WaypointSpacing));
            for (int i = 0; i <= steps; i++)
                path.Add(Vector3.Lerp(from, to, i / (float)steps));
            return true;
        }

        public bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth) => true;

        public bool TryGetFloorHeight(Vector3 position, out float floorHeight)
        {
            floorHeight = 0f;
            return true;
        }
    }
}

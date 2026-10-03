using System.Collections.Generic;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Lets the director's navigation service (Alarm assembly) use the castle graph. Alarm cannot
    /// reference Castle, because Castle references Alarm, so the service is written against
    /// <see cref="IGuardNavigationMap"/> and this adapter is what the castle hands it.
    /// </summary>
    public sealed class CastleGuardNavigationMap : IGuardNavigationMap
    {
        // Within this of a floor cell counts as standing on it, for settling height.
        private const float FloorSearchDistance = 1.5f;
        private const float SightSampleSpacing = 0.25f;

        private readonly CastleNavGraph _graph;

        public CastleGuardNavigationMap(CastleNavGraph graph)
        {
            _graph = graph;
        }

        public int Version => _graph.DoorVersion;

        public int FindCell(Vector3 position) => _graph.NearestWalkableCell(position);

        public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure)
        {
            failure = BlockedReason.NoWalkableCell;
            int startCell = _graph.NearestWalkableCell(from), goalCell = _graph.NearestWalkableCell(to);
            if (startCell < 0 || goalCell < 0)
                return false;
            if (!_graph.IsReachableWithDoorsOpen(startCell, goalCell))
            {
                failure = BlockedReason.Unreachable;
                return false;
            }
            if (_graph.FindPath(from, to, path))
                return true;
            failure = BlockedReason.DoorClosed;
            return false;
        }

        public bool TryGetFloorHeight(Vector3 position, out float floorHeight)
        {
            int cell = _graph.NearestWalkableCell(position, FloorSearchDistance);
            floorHeight = cell >= 0 ? _graph.CellPosition(cell).y : 0f;
            return cell >= 0;
        }

        /// <summary>
        /// Samples along the line and to either side of it: every sample must have a floor cell close by
        /// at about the height the line has there. A line that clips a wall corner or leaves a ledge
        /// fails; so does a slope steeper than one step per sample, which keeps stairs on the cell chain.
        /// </summary>
        public bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth)
        {
            Vector3 line = to - from;
            float length = line.magnitude;
            if (length < SightSampleSpacing)
                return true;
            Vector3 side = Vector3.Cross(Vector3.up, line / length) * halfWidth;
            int samples = Mathf.CeilToInt(length / SightSampleSpacing);
            for (int i = 1; i < samples; i++)
            {
                Vector3 point = Vector3.Lerp(from, to, i / (float)samples);
                if (!HasFloorNear(point) || !HasFloorNear(point + side) || !HasFloorNear(point - side))
                    return false;
            }
            return true;
        }

        // The sample sits at the line's height; a floor cell further off than half a cell plus a
        // slope allowance is a different level or no floor at all.
        private bool HasFloorNear(Vector3 point)
        {
            return _graph.NearestWalkableCell(point, CastleNavTile.CellSize * 1.2f) >= 0;
        }
    }
}

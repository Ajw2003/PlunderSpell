using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// What the navigation service needs to know about the castle. It is an interface because the
    /// director lives in the Alarm assembly and the castle graph in Castle, which already references
    /// Alarm: Alarm cannot name the graph, so Castle implements this (CastleGuardNavigationMap) and
    /// hands it in.
    /// </summary>
    public interface IGuardNavigationMap
    {
        /// <summary>Changes whenever a door opens, closes or gets dearer, so cached routes can be dropped.</summary>
        int Version { get; }

        /// <summary>Id of the walkable cell nearest a point, or -1. Used as the cache key for routes.</summary>
        int FindCell(Vector3 position);

        /// <summary>Fills <paramref name="path"/> with waypoints from one point to another, or says why not.</summary>
        bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path, out BlockedReason failure);

        /// <summary>True when a body <paramref name="halfWidth"/> either side of the straight line stays on walkable floor.</summary>
        bool IsWalkClear(Vector3 from, Vector3 to, float halfWidth);

        /// <summary>The floor height under a point, so movers can settle onto stairs and drop off ledges.</summary>
        bool TryGetFloorHeight(Vector3 position, out float floorHeight);
    }
}

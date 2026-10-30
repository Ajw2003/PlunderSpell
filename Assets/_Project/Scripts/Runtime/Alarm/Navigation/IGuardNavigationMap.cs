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

        /// <summary>
        /// The floor point nearest <paramref name="target"/> that a guard standing at <paramref name="from"/> can walk to
        /// (doors ignored), for a guard whose route to the target failed. False when none lies within a short search radius.
        /// </summary>
        bool TryFindNearestReachable(Vector3 from, Vector3 target, out Vector3 reachable);

        /// <summary>
        /// The first doorway or hole on the route from <paramref name="from"/> toward <paramref name="to"/>, where the route
        /// leaves the room it starts in. False when there is no route or both points share a room. Read-only.
        /// </summary>
        bool TryFindFirstDoorway(Vector3 from, Vector3 to, out Vector3 doorway);

        /// <summary>
        /// How many room changes the route from <paramref name="from"/> to <paramref name="to"/> makes (0 when both share a room),
        /// or -1 when there is no route. Read-only.
        /// </summary>
        int RoomsBetween(Vector3 from, Vector3 to);

        /// <summary>The floor height under a point, so movers can settle onto stairs and drop off ledges.</summary>
        bool TryGetFloorHeight(Vector3 position, out float floorHeight);
    }
}

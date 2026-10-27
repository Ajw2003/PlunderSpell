using UnityEngine;

namespace Plunderspell.Castle
{
    public enum PlannedRoomKind { Room, StairUp, StairDown, FinalChamber }

    /// <summary>One room the floor planner decided on: where, which storey, which zone, what kind.</summary>
    public readonly struct PlannedRoom
    {
        public PlannedRoom(Vector2Int cell, int level, CastleZone zone, PlannedRoomKind kind, Vector2Int exitFacing)
        {
            Cell = cell;
            Level = level;
            Zone = zone;
            Kind = kind;
            ExitFacing = exitFacing;
        }

        public Vector2Int Cell { get; }
        public int Level { get; }
        public CastleZone Zone { get; }
        public PlannedRoomKind Kind { get; }
        /// <summary>For a stair: the direction it opens toward on its exit level. Zero otherwise.</summary>
        public Vector2Int ExitFacing { get; }
    }
}

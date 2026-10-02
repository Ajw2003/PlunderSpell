using UnityEngine;

namespace Plunderspell.Alarm
{
    // The navigation service's event payloads (#222). Readonly structs like the rest of the director's
    // bus, so raising one never allocates. A guard is carried as a Component because the Alarm assembly
    // sits below Guards.

    /// <summary>Why a guard is being sent somewhere. Carried through so listeners can tell a patrol from a chase.</summary>
    public enum MoveReason
    {
        Patrol,
        Investigate,
        Chase,
        Return
    }

    /// <summary>Why a move stopped short of its destination.</summary>
    public enum BlockedReason
    {
        /// <summary>No navigation map has been given to the service.</summary>
        NoMap,
        /// <summary>The guard or the destination is not near any walkable floor.</summary>
        NoWalkableCell,
        /// <summary>No route exists even with every door open.</summary>
        Unreachable,
        /// <summary>A route exists, but only through a door that is closed.</summary>
        DoorClosed,
        /// <summary>The sweep found a wall or a player in the way and the guard could not get past.</summary>
        Obstacle
    }

    /// <summary>Asks the navigation service to walk a guard to a point.</summary>
    public readonly struct MoveRequest
    {
        public readonly Component Guard;
        public readonly Vector3 Destination;
        /// <summary>Metres per second.</summary>
        public readonly float Speed;
        public readonly MoveReason Reason;

        public MoveRequest(Component guard, Vector3 destination, float speed, MoveReason reason)
        {
            Guard = guard;
            Destination = destination;
            Speed = speed;
            Reason = reason;
        }
    }

    /// <summary>A route to the destination exists and the guard has started along it.</summary>
    public readonly struct PathReady
    {
        public readonly Component Guard;
        public readonly Vector3 Destination;
        public readonly int WaypointCount;
        /// <summary>Walking length of the smoothed route, in metres.</summary>
        public readonly float Length;

        public PathReady(Component guard, Vector3 destination, int waypointCount, float length)
        {
            Guard = guard;
            Destination = destination;
            WaypointCount = waypointCount;
            Length = length;
        }
    }

    /// <summary>The guard reached its destination.</summary>
    public readonly struct Arrived
    {
        public readonly Component Guard;
        public readonly Vector3 Position;

        public Arrived(Component guard, Vector3 position)
        {
            Guard = guard;
            Position = position;
        }
    }

    /// <summary>The guard cannot get to its destination. It stands where it is until asked again.</summary>
    public readonly struct Blocked
    {
        public readonly Component Guard;
        public readonly Vector3 Position;
        public readonly BlockedReason Reason;

        public Blocked(Component guard, Vector3 position, BlockedReason reason)
        {
            Guard = guard;
            Position = position;
            Reason = reason;
        }
    }
}

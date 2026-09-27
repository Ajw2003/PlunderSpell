using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>The kind of opening a socket represents. Only compatible types may connect.</summary>
    public enum SocketType
    {
        Door,
        Window,
        MurderHole,
        Staircase,
        WallSegment,
        ArchOpening
    }

    /// <summary>
    /// The outward facing direction of a socket, in the module's local space.
    /// Up/Down are used for vertical (staircase) connections between zones.
    /// </summary>
    public enum Direction
    {
        North,
        South,
        East,
        West,
        Up,
        Down
    }

    /// <summary>
    /// A tagged connection point on a <see cref="CastleRoomModule"/>. The procedural generator
    /// matches an unoccupied socket on an already-placed module with a compatible socket on a
    /// new module, rotating the new module so the two sockets face each other.
    /// </summary>
    [DisallowMultipleComponent]
    public class SocketPoint : MonoBehaviour
    {
        [Tooltip("The opening type this socket exposes.")]
        public SocketType Type = SocketType.Door;

        [Tooltip("Local facing direction of this socket.")]
        public Direction Facing = Direction.North;

        [Tooltip("True once this socket has been paired with another socket by the generator.")]
        public bool IsOccupied;

        [Tooltip("The socket this one is connected to (null when free).")]
        public SocketPoint ConnectedTo;

        /// <summary>
        /// Returns the world-space unit vector for a local <see cref="Direction"/>, taking the
        /// owning transform's rotation into account so rotated modules resolve correctly.
        /// </summary>
        public Vector3 WorldDirection => transform.TransformDirection(LocalDirectionVector(Facing));

        /// <summary>Converts a <see cref="Direction"/> enum to a local unit vector.</summary>
        public static Vector3 LocalDirectionVector(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return Vector3.forward;
                case Direction.South: return Vector3.back;
                case Direction.East: return Vector3.right;
                case Direction.West: return Vector3.left;
                case Direction.Up: return Vector3.up;
                case Direction.Down: return Vector3.down;
                default: return Vector3.forward;
            }
        }

        /// <summary>Returns the direction facing the opposite way (used to line sockets up).</summary>
        public static Direction Opposite(Direction dir)
        {
            switch (dir)
            {
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                case Direction.West: return Direction.East;
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                default: return Direction.South;
            }
        }

        /// <summary>
        /// Determines whether two sockets can be joined. Both must be currently unoccupied and
        /// their types must form an allowed pair:
        /// Door↔Door, Staircase↔Staircase, Window↔Window, ArchOpening↔ArchOpening,
        /// and MurderHole↔WallSegment (a murder hole cut into a wall segment).
        /// </summary>
        public static bool AreCompatible(SocketPoint a, SocketPoint b)
        {
            if (a == null || b == null)
                return false;
            if (a.IsOccupied || b.IsOccupied)
                return false;
            if (a == b)
                return false;

            return AreTypesCompatible(a.Type, b.Type);
        }

        /// <summary>Type-only compatibility check (ignores occupancy), useful for pooling/planning.</summary>
        public static bool AreTypesCompatible(SocketType a, SocketType b)
        {
            switch (a)
            {
                case SocketType.Door: return b == SocketType.Door;
                case SocketType.Window: return b == SocketType.Window;
                case SocketType.Staircase: return b == SocketType.Staircase;
                case SocketType.ArchOpening: return b == SocketType.ArchOpening;
                case SocketType.MurderHole: return b == SocketType.WallSegment;
                case SocketType.WallSegment: return b == SocketType.MurderHole;
                default: return false;
            }
        }

        /// <summary>Marks two sockets as paired, wiring their back-references.</summary>
        public static void Connect(SocketPoint a, SocketPoint b)
        {
            if (a == null || b == null)
                return;
            a.IsOccupied = true;
            b.IsOccupied = true;
            a.ConnectedTo = b;
            b.ConnectedTo = a;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            Gizmos.DrawLine(transform.position, transform.position + WorldDirection * 0.75f);
        }
    }
}

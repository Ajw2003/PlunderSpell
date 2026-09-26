using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Concentric castle zones, ordered innermost (Crypt) to outermost (CurtainWall).
    /// The generator places rings from the center outward in reverse of this ordering.
    /// </summary>
    public enum CastleZone
    {
        CurtainWall,
        OuterBailey,
        InnerWard,
        Keep,
        Crypt
    }

    /// <summary>
    /// Runtime component placed on every room prefab. Holds the room's zone, identifier and the
    /// list of <see cref="SocketPoint"/>s (auto-collected from children). The generator writes the
    /// grid position after placement and flags extraction/crypt rooms.
    /// </summary>
    [DisallowMultipleComponent]
    public class CastleRoomModule : MonoBehaviour
    {
        [Tooltip("Which concentric zone this module belongs to.")]
        public CastleZone Zone = CastleZone.OuterBailey;

        [Tooltip("Stable identifier matching the registry entry (e.g. GatehouseModule).")]
        public string RoomId;

        [Tooltip("Sockets exposed by this module. Auto-populated from children on Awake.")]
        public List<SocketPoint> Sockets = new List<SocketPoint>();

        [Tooltip("Grid cell assigned by the generator after placement.")]
        public Vector2Int GridPosition;

        [Tooltip("True if this module contains the extraction exit socket.")]
        public bool IsExtractionExit;

        [Tooltip("True if this module is the crypt entry / final loot chamber.")]
        public bool IsCryptEntry;

        private void Awake()
        {
            PopulateSockets();
        }

        /// <summary>
        /// Rebuilds <see cref="Sockets"/> from all <see cref="SocketPoint"/> components found in
        /// children. Safe to call from editor tooling as well as at runtime.
        /// </summary>
        public void PopulateSockets()
        {
            Sockets.Clear();
            GetComponentsInChildren(true, Sockets);
        }

        /// <summary>Returns the first unoccupied socket of the requested type, or null.</summary>
        public SocketPoint FindFreeSocket(SocketType type)
        {
            for (int i = 0; i < Sockets.Count; i++)
            {
                SocketPoint s = Sockets[i];
                if (s != null && !s.IsOccupied && s.Type == type)
                    return s;
            }
            return null;
        }

        /// <summary>Returns every unoccupied socket on this module.</summary>
        public IEnumerable<SocketPoint> FreeSockets()
        {
            for (int i = 0; i < Sockets.Count; i++)
            {
                SocketPoint s = Sockets[i];
                if (s != null && !s.IsOccupied)
                    yield return s;
            }
        }
    }
}

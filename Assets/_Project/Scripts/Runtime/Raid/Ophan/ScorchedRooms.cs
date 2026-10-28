using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Rooms the Ophan's charged beam has scorched this raid (#442): their walls are drawn burnt. The beam's hole is cut
    /// by <see cref="OphanWallCut"/> (#467); a scorched room's other walls still stop the slow beam. A room is named by its place
    /// among the generated castle's children, which is the same on every machine because every machine builds the
    /// castle from the same seed. Cleared when a new castle is built.
    /// </summary>
    public static class ScorchedRooms
    {
        private static readonly Color Burnt = new Color(0.18f, 0.12f, 0.1f);
        private static readonly HashSet<CastleRoomModule> s_scorched = new HashSet<CastleRoomModule>();
        private static MaterialPropertyBlock s_block;

        public static int Count => s_scorched.Count;

        /// <summary>The room a collider belongs to, or null for anything that is not a castle room.</summary>
        public static CastleRoomModule RoomOf(Collider collider) =>
            collider != null ? collider.GetComponentInParent<CastleRoomModule>() : null;


        /// <summary>The room's place in the generated castle, sent over the network instead of the room itself.</summary>
        public static int IndexOf(CastleRoomModule room) => room.transform.GetSiblingIndex();

        /// <summary>Finds the room at <paramref name="index"/> under the generated castle on this machine.</summary>
        public static CastleRoomModule RoomAt(int index)
        {
            var any = Object.FindFirstObjectByType<CastleRoomModule>();
            Transform container = any != null ? any.transform.parent : null;
            if (container == null || index < 0 || index >= container.childCount)
                return null;
            return container.GetChild(index).GetComponent<CastleRoomModule>();
        }

        /// <summary>Marks the room scorched and darkens it. Doing it twice changes nothing.</summary>
        public static void Scorch(CastleRoomModule room)
        {
            if (room == null || !s_scorched.Add(room))
                return;
            s_block ??= new MaterialPropertyBlock();
            foreach (Renderer renderer in room.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(s_block);
                s_block.SetColor("_BaseColor", Burnt);
                s_block.SetColor("_Color", Burnt);
                renderer.SetPropertyBlock(s_block);
            }
        }

        public static void Clear() => s_scorched.Clear();
    }
}

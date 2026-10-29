using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Rooms the Ophan's charged beam has scorched this raid (#442). The beam's hole is cut
    /// by the fracture system (#472); a scorched room's other walls still stop the slow beam. A room is named by its place
    /// among the generated castle's children, which is the same on every machine because every machine builds the
    /// castle from the same seed. Cleared when a new castle is built.
    /// </summary>
    public static class ScorchedRooms
    {
        private static readonly HashSet<CastleRoomModule> s_scorched = new HashSet<CastleRoomModule>();

        public static int Count => s_scorched.Count;

        // The generated castle's room container, found once per castle: each charged shot looked it up with
        // FindFirstObjectByType for every hit, up to 128 times (audit P3, #526). Unity's null check drops a destroyed one.
        private static Transform s_container;

        private static Transform Container()
        {
            if (s_container == null)
            {
                var any = Object.FindFirstObjectByType<CastleRoomModule>();
                s_container = any != null ? any.transform.parent : null;
            }
            return s_container;
        }

        /// <summary>The room a collider belongs to, or null for anything that is not a castle room.</summary>
        public static CastleRoomModule RoomOf(Collider collider) =>
            collider != null ? collider.GetComponentInParent<CastleRoomModule>() : null;


        /// <summary>The room's place in the generated castle, sent over the network instead of the room itself.</summary>
        public static int IndexOf(CastleRoomModule room) => room.transform.GetSiblingIndex();

        /// <summary>Finds the room at <paramref name="index"/> under the generated castle on this machine.</summary>
        public static CastleRoomModule RoomAt(int index)
        {
            Transform container = Container();
            if (container == null || index < 0 || index >= container.childCount)
                return null;
            return container.GetChild(index).GetComponent<CastleRoomModule>();
        }

        /// <summary>The top-level castle piece (room, door plug, anything) a collider sits in, as its place under the room
        /// container; -1 when it is not under the container.</summary>
        public static int PieceIndexOf(Collider collider)
        {
            Transform container = Container();
            if (container == null || collider == null)
                return -1;
            for (Transform t = collider.transform; t != null; t = t.parent)
                if (t.parent == container)
                    return t.GetSiblingIndex();
            return -1;
        }

        /// <summary>The castle piece at <paramref name="index"/> under the room container, or null.</summary>
        public static Transform PieceAt(int index)
        {
            Transform container = Container();
            return container == null || index < 0 || index >= container.childCount ? null : container.GetChild(index);
        }

        /// <summary>Marks the room scorched. It is no longer darkened: the breaking replaced that look, and the tint left
        /// the standing walls as one flat dark slab (aj, 2026-10-28). Doing it twice changes nothing.</summary>
        public static void Scorch(CastleRoomModule room)
        {
            if (room != null)
                s_scorched.Add(room);
        }

        public static void Clear()
        {
            s_scorched.Clear();
            s_container = null;
        }
    }
}

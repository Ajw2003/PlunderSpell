using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Tells the nav graph what the lockdown did to the doors (#222, bespoke-navigation.md Design 5). A
    /// barred door closes its archway, so guards route round it or report it blocked. A locked door
    /// only costs: guards still use it, but prefer a way round if one is not much longer.
    /// </summary>
    public static class CastleLockdownNavigation
    {
        /// <summary>Walking metres a guard will detour to avoid a locked door.</summary>
        public const float LockedDoorCost = 40f;

        /// <summary>A door sits in its archway, so its link is within a metre or two of it.</summary>
        public const float DoorToArchwayDistance = 2f;

        /// <summary>Marks one door's archway from the door's own state. Returns false if no archway is near the door.</summary>
        public static bool MarkDoor(CastleNavGraph graph, CastleDoor door)
        {
            int link = graph.FindLinkNear(door.transform.position, DoorToArchwayDistance);
            if (link < 0)
                return false;
            graph.SetDoorCost(link, CostOf(door));
            return true;
        }

        private static float CostOf(CastleDoor door)
        {
            if (door.IsBarred)
                return CastleNavGraph.ClosedDoor;
            return door.IsLocked ? LockedDoorCost : 0f;
        }
    }
}

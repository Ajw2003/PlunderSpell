using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Says whether a collider hides a player from a guard (#482). Only castle stone does: loot, other guards
    /// and anything under a room's Props part (the "Part_Props" child RoomPartSplitter makes) let sight through.
    /// </summary>
    public static class GuardSightBlockers
    {
        /// <summary>The name of a room's Props part.</summary>
        public const string PropsPartName = "Part_Props";

        public static bool Blocks(Collider collider)
        {
            return collider.GetComponentInParent<LootPickup>() == null
                && collider.GetComponentInParent<Guard>() == null
                && !IsUnderPropsPart(collider.transform);
        }

        private static bool IsUnderPropsPart(Transform hit)
        {
            for (Transform step = hit; step != null; step = step.parent)
            {
                if (step.name == PropsPartName)
                    return true;
            }
            return false;
        }
    }
}

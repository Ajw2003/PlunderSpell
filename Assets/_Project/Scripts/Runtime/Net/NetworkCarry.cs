using PurrNet;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Installs the session rules for moving physical items (<see cref="Item.CanDriveHere"/>,
    /// <see cref="Item.RequestDrive"/>): a machine may only drag a networked item whose transform it
    /// controls, and asks the server for ownership of loot otherwise. Items with no
    /// NetworkTransform (weapons, which each machine has its own copy of) are always free to move.
    /// </summary>
    public static class NetworkCarry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            Item.CanDriveHere = CanDriveHere;
            Item.RequestDrive = RequestDrive;
        }

        private static bool CanDriveHere(Item item)
        {
            if (item == null || !item.TryGetComponent(out NetworkTransform synced) || !synced.isSpawned)
                return true;
            return synced.IsController(synced.ownerAuth);
        }

        private static void RequestDrive(Item item)
        {
            if (item != null && item.TryGetComponent(out LootPickup pickup) && pickup.isSpawned)
                pickup.RequestCarry();
        }
    }
}

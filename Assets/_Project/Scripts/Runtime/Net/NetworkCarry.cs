using PurrNet;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Installs the session rules for moving physical items (<see cref="Item.CanDriveHere"/>,
    /// <see cref="Item.RequestDrive"/>, <see cref="Item.RequestThrow"/>): a machine may only drag a
    /// networked item whose transform it controls. A weapon held in the hand asks for ownership, as
    /// before; a piece on the beam instead asks the server to take control (drop its owner), since
    /// while held it has none and the server applies every holder's pull. A let-go throw from a
    /// machine that does not control the body is likewise sent to the server to apply. An item with
    /// no spawned NetworkTransform (offline) is always free to move.
    /// </summary>
    public static class NetworkCarry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Install()
        {
            Item.CanDriveHere = CanDriveHere;
            Item.RequestDrive = RequestDrive;
            Item.RequestThrow = RequestThrow;
            Item.RequestMove = RequestMove;
            Item.NetworkedTotalGrip = NetworkedTotalGrip;
        }

        private static bool CanDriveHere(Item item)
        {
            if (item == null || !item.TryGetComponent(out NetworkTransform synced) || !synced.isSpawned)
                return true;
            return synced.IsController(synced.ownerAuth);
        }

        private static bool IsHeldInHand(Item item) =>
            item.TryGetComponent(out RangedWeapon _) || item.TryGetComponent(out MeleeWeapon _);

        private static void RequestDrive(Item item)
        {
            if (item == null || !item.TryGetComponent(out LootPickup pickup) || !pickup.isSpawned)
                return;
            if (IsHeldInHand(item))
                pickup.RequestCarry();
            else
                pickup.RequestHostControl();
        }

        private static void RequestMove(Item item, Vector3 position, Quaternion rotation)
        {
            CarryBeamRelay relay = Object.FindFirstObjectByType<CarryBeamRelay>();
            if (relay != null)
                relay.MoveCarried(item, position, rotation);
        }

        private static void RequestThrow(Item item, Vector3 direction, float force)
        {
            if (item != null && item.TryGetComponent(out LootPickup pickup) && pickup.isSpawned)
                pickup.RequestThrow(direction, force);
        }

        /// <summary>A client holder's view of the combined grip a controller (the host) already
        /// knows: the server-written SyncVar on the spawned LootPickup, or 0 (Item.TotalGrip's
        /// "nothing heard yet" fallback) offline or unspawned.</summary>
        private static float NetworkedTotalGrip(Item item)
        {
            if (item == null || !item.TryGetComponent(out LootPickup pickup) || !pickup.isSpawned)
                return 0f;
            return pickup.TotalGrip;
        }
    }
}

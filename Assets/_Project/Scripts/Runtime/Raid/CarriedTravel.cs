using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Takes what the local player holds on the beam (<see cref="ItemManager.CarriedItem"/>) through a
    /// room change with them (#333): same offset from the body, turned by however far the player was
    /// turned, no velocity, still held. Used by <see cref="RoomTravel"/>.
    /// </summary>
    public static class CarriedTravel
    {
        /// <summary>The held piece, or null. A piece anyone else also holds is let go instead.</summary>
        public static Item Find()
        {
            ItemManager items = ItemManager.Instance;
            Item held = items != null ? items.CarriedItem : null;
            if (held == null || held.IsInHand)
                return null;
            if (held.HolderCount > 1)
            {
                Debug.Log($"[RoomTravel] {held.name} is held by more than one player; {held.name} stays behind and is let go.");
                items.ForceRelease();
                return null;
            }
            return held;
        }

        /// <summary>Where a piece lands when its holder moves from <paramref name="oldBody"/> to
        /// <paramref name="newBody"/> and turns by <paramref name="yawDelta"/> degrees.</summary>
        public static Pose Moved(Pose piece, Vector3 oldBody, Vector3 newBody, float yawDelta)
        {
            Quaternion turn = Quaternion.Euler(0f, yawDelta, 0f);
            return new Pose(newBody + turn * (piece.position - oldBody), turn * piece.rotation);
        }

        public static void Place(Item piece, Pose pose)
        {
            if (Item.CanDriveHere(piece))
            {
                piece.TeleportTo(pose.position, pose.rotation);
                return;
            }
            // A client holder: the server controls the body, so it moves it and replicates the move.
            piece.ForgetTargetMotion();
            Item.RequestMove?.Invoke(piece, pose.position, pose.rotation);
        }
    }
}

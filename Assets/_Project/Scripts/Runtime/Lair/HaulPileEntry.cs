using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// One saved haul piece: its loot asset name and, when known, where it lay. Name-only entries
    /// (saves from before poses, and unresolved names) have no pose and are placed by the grid.
    /// </summary>
    public readonly struct HaulPileEntry
    {
        public readonly string Name;
        public readonly bool HasPose;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;

        public HaulPileEntry(string name)
        {
            Name = name;
            HasPose = false;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }

        public HaulPileEntry(string name, Vector3 position, Quaternion rotation)
        {
            Name = name;
            HasPose = true;
            Position = position;
            Rotation = rotation;
        }
    }
}

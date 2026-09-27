using System;
using System.Collections.Generic;
using Plunderspell.Castle;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// What can be found where. One entry per lootable object, tagged with the castle zone it belongs
    /// to and a weight for how commonly it turns up there.
    ///
    /// The zone tagging is the whole risk curve of a raid: the valuable things are in the Keep and
    /// the Crypt, which are the deepest rooms and the longest carry back out.
    /// </summary>
    [CreateAssetMenu(fileName = "RaidLootTable", menuName = "Plunderspell/Raid Loot Table")]
    public class RaidLootTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The loot asset to place.")]
            public LootItem Item;

            [Tooltip("Which zone this item is found in.")]
            public CastleZone Zone = CastleZone.OuterBailey;

            [Tooltip("Relative frequency within its zone. Higher is more common.")]
            [Min(1f)]
            public int Weight = 10;

            [Tooltip("Prefab spawned for this item. Optional: without one the placement is data-only.")]
            public GameObject Prefab;
        }

        [Tooltip("Every lootable object, tagged by the zone it is found in.")]
        public List<Entry> Entries = new List<Entry>();

        [Header("Density")]
        [Tooltip("Chance (0..1) that a given room in each zone contains loot at all.")]
        [Range(0f, 1f)] public float CurtainWallDensity = 0.15f;
        [Range(0f, 1f)] public float OuterBaileyDensity = 0.5f;
        [Range(0f, 1f)] public float InnerWardDensity = 1.0f;
        [Range(0f, 1f)] public float KeepDensity = 1.0f;
        [Range(0f, 1f)] public float CryptDensity = 1.0f;

        [Header("Items per looted room")]
        [Tooltip("Most items a looted room in each zone holds; it holds 1 to this many, each on its " +
                 "own loot anchor, never more than the room has. The crypt centre fills every anchor.")]
        [Min(1)] public int CurtainWallMaxPerRoom = 1;
        [Min(1)] public int OuterBaileyMaxPerRoom = 1;
        [Min(1)] public int InnerWardMaxPerRoom = 2;
        [Min(1)] public int KeepMaxPerRoom = 3;

        /// <summary>Chance that a room in <paramref name="zone"/> holds loot.</summary>
        public float DensityFor(CastleZone zone)
        {
            switch (zone)
            {
                case CastleZone.CurtainWall: return CurtainWallDensity;
                case CastleZone.OuterBailey: return OuterBaileyDensity;
                case CastleZone.InnerWard: return InnerWardDensity;
                case CastleZone.Keep: return KeepDensity;
                case CastleZone.Crypt: return CryptDensity;
                default: return 0f;
            }
        }

        /// <summary>Most items a looted room in <paramref name="zone"/> holds.</summary>
        public int MaxPerRoomFor(CastleZone zone)
        {
            switch (zone)
            {
                case CastleZone.CurtainWall: return Mathf.Max(1, CurtainWallMaxPerRoom);
                case CastleZone.OuterBailey: return Mathf.Max(1, OuterBaileyMaxPerRoom);
                case CastleZone.InnerWard: return Mathf.Max(1, InnerWardMaxPerRoom);
                case CastleZone.Keep: return Mathf.Max(1, KeepMaxPerRoom);
                default: return 1;
            }
        }

        /// <summary>Every entry belonging to a zone, in authored order.</summary>
        public List<Entry> EntriesFor(CastleZone zone)
        {
            var result = new List<Entry>();
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry e = Entries[i];
                if (e != null && e.Zone == zone && e.Item != null)
                    result.Add(e);
            }
            return result;
        }
    }
}

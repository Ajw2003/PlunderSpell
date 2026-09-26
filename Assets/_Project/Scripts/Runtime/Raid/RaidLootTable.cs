using System;
using System.Collections.Generic;
using RogueAi.Castle;
using RogueAi.Loot;
using UnityEngine;

namespace RogueAi.Raid
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
        [Range(0f, 1f)] public float OuterBaileyDensity = 0.35f;
        [Range(0f, 1f)] public float InnerWardDensity = 0.5f;
        [Range(0f, 1f)] public float KeepDensity = 0.7f;
        [Range(0f, 1f)] public float CryptDensity = 1.0f;

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

using System;
using System.Collections.Generic;
using Plunderspell.Castle;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Which rooms, loot and garrison each historical era is raided with. An empty field falls
    /// back to the scene's own assignment.
    /// </summary>
    // doc-ref da27 docs/systems/raid-scene-assembly.md
    [CreateAssetMenu(fileName = "EraContentCatalogue", menuName = "Plunderspell/Era Content Catalogue")]
    public class EraContentCatalogue : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("The era this content is raided in.")]
            public HistoricalEra Era;

            [Tooltip("Room set for this era. Empty: the generator's own registry.")]
            public CastleRoomRegistry Rooms;

            [Tooltip("Plunder for this era. Empty: the loot spawner's own table.")]
            public RaidLootTable Loot;

            [Tooltip("Garrison for this era. Empty: the guard spawner's own roster.")]
            public EnemyRoster Enemies;
        }

        [Tooltip("One entry per era. An era with no entry raids with the scene's defaults.")]
        public List<Entry> Entries = new List<Entry>();

        /// <summary>The entry for <paramref name="era"/>, or null when it has none.</summary>
        public Entry For(HistoricalEra era)
        {
            for (int i = 0; i < Entries.Count; i++)
            {
                if (Entries[i] != null && Entries[i].Era == era)
                    return Entries[i];
            }
            return null;
        }
    }
}

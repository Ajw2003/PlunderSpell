using System;
using System.Collections.Generic;
using RogueAi.Castle;
using RogueAi.Inventory;
using UnityEngine;

namespace RogueAi.Raid
{
    /// <summary>
    /// Which enemy stands where. One entry per enemy prefab, tagged with the castle zone it garrisons
    /// and a weight for how commonly it turns up there.
    ///
    /// This is the garrison's half of the risk curve that <see cref="RaidLootTable"/> draws for loot:
    /// the deep rooms hold the valuable things, and they hold the dangerous things for the same
    /// reason. A single enemy prefab would have made every room the same encounter.
    ///
    /// <see cref="GuardPlacementPlanner"/> already decides how many guards stand where and what they
    /// walk; the roster only answers "which one", so the plan stays a pure function of the seed.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyRoster", menuName = "Plunderspell/Enemy Roster")]
    public class EnemyRoster : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Stable identifier, matching the source model name.")]
            public string EnemyId;

            [Tooltip("Which zone this enemy garrisons.")]
            public CastleZone Zone = CastleZone.OuterBailey;

            [Tooltip("The Age this enemy belongs to. A raid draws only enemies of its own Age.")]
            public HistoricalEra Era = HistoricalEra.HighMedieval;

            [Tooltip("Spawns in every Age. Only for the supernatural Crypt enemies, which belong to " +
                     "no century; see docs/systems/raid-scene-assembly.md (\"Era reaches the raid\").")]
            public bool AnyEra;

            [Tooltip("Relative frequency within its zone. Higher is more common.")]
            [Min(1)]
            public int Weight = 10;

            [Tooltip("Prefab spawned for this enemy.")]
            public GameObject Prefab;

            /// <summary>True when this entry may spawn in a raid set in <paramref name="era"/>.</summary>
            public bool BelongsTo(HistoricalEra era) => AnyEra || Era == era;
        }

        [Tooltip("Every enemy prefab, tagged by the zone it garrisons.")]
        public List<Entry> Entries = new List<Entry>();

        /// <summary>Every spawnable entry belonging to a zone, in authored order.</summary>
        public List<Entry> EntriesFor(CastleZone zone)
        {
            var result = new List<Entry>();
            for (int i = 0; i < Entries.Count; i++)
            {
                Entry entry = Entries[i];
                if (entry != null && entry.Zone == zone && entry.Prefab != null)
                    result.Add(entry);
            }
            return result;
        }

        /// <summary>Every spawnable entry for a zone that belongs to an Age, in authored order.</summary>
        public List<Entry> EntriesFor(CastleZone zone, HistoricalEra era)
        {
            List<Entry> result = EntriesFor(zone);
            result.RemoveAll(entry => !entry.BelongsTo(era));
            return result;
        }

        // Which (zone, era) pairs have already warned about falling back, so a raid of forty guards
        // logs the gap once per zone rather than forty times. Cleared per raid by ResetWarnings.
        [NonSerialized] private HashSet<(CastleZone, HistoricalEra)> _warnedFallbacks;

        /// <summary>
        /// Weighted pick from the zone's pool for an Age, or null when the zone has no entries at
        /// all. Filters by era first; when no enemy of that Age garrisons the zone, falls back to the
        /// zone's whole pool and logs a warning, because an empty room would hide the gap and a
        /// wrong-era guard shows it. Takes the caller's RNG so the choice stays part of the
        /// seed-derived plan rather than a separate random source.
        /// </summary>
        public GameObject PickForZone(CastleZone zone, HistoricalEra era, System.Random rng)
        {
            List<Entry> pool = EntriesFor(zone, era);
            if (pool.Count == 0)
            {
                pool = EntriesFor(zone);
                if (pool.Count == 0)
                    return null;
                WarnFallback(zone, era, pool.Count);
            }

            return PickWeighted(pool, rng).Prefab;
        }

        /// <summary>Forgets which fallbacks were already reported, so the next raid reports its own.</summary>
        public void ResetWarnings() => _warnedFallbacks?.Clear();

        /// <summary>Weighted pick from a non-empty pool; weights under 1 count as 1.</summary>
        public static Entry PickWeighted(List<Entry> pool, System.Random rng)
        {
            int total = 0;
            for (int i = 0; i < pool.Count; i++)
                total += Mathf.Max(1, pool[i].Weight);

            int roll = rng.Next(0, total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(1, pool[i].Weight);
                if (roll < 0)
                    return pool[i];
            }
            return pool[pool.Count - 1];
        }

        private void WarnFallback(CastleZone zone, HistoricalEra era, int poolSize)
        {
            _warnedFallbacks ??= new HashSet<(CastleZone, HistoricalEra)>();
            if (!_warnedFallbacks.Add((zone, era)))
                return;
            Debug.LogWarning($"[Roster] No {era} enemy garrisons {zone} in '{name}'; falling back to " +
                             $"the zone's {poolSize} entr{(poolSize == 1 ? "y" : "ies")} from any Age. " +
                             "Run Tools/Plunderspell/Forge Art Bible Enemies to post that Age's enemies.");
        }
    }
}

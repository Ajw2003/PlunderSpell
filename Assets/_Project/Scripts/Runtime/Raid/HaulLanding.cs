using System.Collections.Generic;
using Code.Scripts.EventSystems;
using Plunderspell.Core;
using Plunderspell.Extraction;
using Plunderspell.Lair;
using Plunderspell.Loot;
using PurrNet;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Lands each raid's haul on the Lair floor at the child "HaulLanding": on the server, the pieces
    /// carried through the portal are spawned there beside the unsold ones already lying about. The
    /// pile persists across raids and is saved per save slot (<see cref="HaulPileSave"/>) as the asset
    /// names of its pieces' LootItems; it is respawned when the server is ready and when another slot
    /// loads. The save is always rebuilt from the pieces that exist, so a piece that leaves the pile
    /// (<see cref="Remove"/>) or is destroyed leaves the save too. Its pieces are its own spawner's, so
    /// the raid's loot clear never touches them. No per-frame code: it saves on events only (a haul landing, a piece
    /// leaving, the company setting out, the session ending, the application quitting). See docs/4-systems/raid.md,
    /// "How the haul comes home".
    /// </summary>
    public class HaulLanding : MonoBehaviour
    {
        [Tooltip("Spawns and owns the pile. Separate from the raid's spawner so the raid's clear leaves it alone.")]
        [SerializeField] private LootSpawner _pile;

        private bool _restored;

        // Saved entries no loot table could resolve: kept in the save (with any pose) so a missing table never erases a piece.
        private readonly List<HaulPileEntry> _unresolved = new List<HaulPileEntry>();

        // One table's share of a restore: posed pieces go back where they were, the rest take the grid.
        private class RestoreBatch
        {
            public readonly List<LootItem> Grid = new List<LootItem>();
            public readonly List<LootItem> Posed = new List<LootItem>();
            public readonly List<Vector3> Positions = new List<Vector3>();
            public readonly List<Quaternion> Rotations = new List<Quaternion>();
        }

        // Saves happen only at these moments, all events: a haul lands, a piece leaves, the company sets out,
        // the session ends, the application quits. Restores: the server starting and a save slot loading.
        /// <summary>The live landing, found without a scene search (scene split, #537).</summary>
        public static HaulLanding Current { get; private set; }

        private void OnEnable()
        {
            Current = this;
            EventManager.Instance?.Subscribe(this, (HaulExtracted e) => Land(e));
            EventManager.Instance?.Subscribe(this, (SaveSlotLoaded e) => Reload());
            EventManager.Instance?.Subscribe(this, (CoopChanged e) => OnCoopChanged());
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnPhaseChanged(e.Phase));
            EventManager.Instance?.Subscribe(this, (SessionEnding e) => SaveIfLive());
            // The server may have started before this object was enabled.
            OnCoopChanged();
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
        }

        // Quitting from the Lair menu is a quit too; scene teardown is not used because pieces may already be gone.
        private void OnApplicationQuit() => SaveIfLive();

        // The server started: restore. The server stopped: its pile is gone but the save is not, so forget the
        // pile and restore from the save when the next session starts.
        private void OnCoopChanged()
        {
            if (!ServerReady())
            {
                _restored = false;
                _unresolved.Clear();
            }
            else if (!_restored)
                Restore();
        }

        private void OnPhaseChanged(RaidPhase phase)
        {
            if (phase == RaidPhase.Generating)
                SaveIfLive();
        }

        // Never saves a pile that was not restored or no longer exists: that would wipe the save.
        private void SaveIfLive()
        {
            if (_restored && _pile != null && ServerReady() && IsServer())
                Save();
        }

        private static bool IsServer() => NetworkManager.main == null || !NetworkManager.main.isClientOnly;

        // Pieces spawned before the server runs would not be networked, so wait for it.
        private static bool ServerReady() => NetworkManager.main == null || NetworkManager.main.isServer;

        /// <summary>Takes a piece out of the pile (picked up for the Market, sold) and saves.</summary>
        public void Remove(GameObject piece)
        {
            if (_pile != null && _pile.Remove(piece))
                Save();
        }

        private void Land(HaulExtracted haul)
        {
            if (!IsServer() || _pile == null)
                return;
            if (!_restored)
                Restore();
            Spawn(haul.Pieces, RaidTable());
            Save();
        }

        // Another slot's pile replaces this one.
        private void Reload()
        {
            if (!IsServer() || _pile == null)
                return;
            _pile.Clear();
            _unresolved.Clear();
            _restored = false;
            if (ServerReady())
                Restore();
        }

        private void Restore()
        {
            _restored = true;
            if (!IsServer() || _pile == null)
                return;

            // A restored pile can mix eras, so pieces are spawned per table to get each one's prefab.
            var byTable = new Dictionary<RaidLootTable, RestoreBatch>();
            foreach (HaulPileEntry entry in HaulPileSave.LoadEntries(SaveSlots.Active))
            {
                LootItem item = Resolve(entry.Name, out RaidLootTable table);
                if (item == null)
                {
                    _unresolved.Add(entry);
                    continue;
                }
                if (!byTable.TryGetValue(table, out RestoreBatch batch))
                    byTable[table] = batch = new RestoreBatch();
                if (!entry.HasPose)
                    batch.Grid.Add(item);
                else
                {
                    batch.Posed.Add(item);
                    batch.Positions.Add(entry.Position);
                    batch.Rotations.Add(entry.Rotation);
                }
            }
            // Posed pieces first, so the grid pieces see their cells as taken. SpawnPile holds them kinematic
            // until its settle delay passes, so physics cannot fling a piece placed on or inside another.
            foreach (KeyValuePair<RaidLootTable, RestoreBatch> group in byTable)
                _pile.SpawnPile(group.Value.Posed, group.Value.Positions, group.Value.Rotations, group.Key);
            foreach (KeyValuePair<RaidLootTable, RestoreBatch> group in byTable)
                Spawn(group.Value.Grid, group.Key);
            Save();
        }

        // Adds pieces beside the ones already there, continuing the grid after them.
        // New pieces take the first free cells, so one removed from the middle of the pile leaves a gap that is refilled.
        private void Spawn(IReadOnlyList<LootItem> pieces, RaidLootTable table)
        {
            var taken = new List<Vector3>();
            foreach (GameObject go in _pile.Spawned)
            {
                if (go != null)
                    taken.Add(transform.InverseTransformPoint(go.transform.position));
            }
            var points = HaulLayout.FreeOffsets(pieces.Count, taken);
            for (int i = 0; i < points.Count; i++)
                points[i] = transform.TransformPoint(points[i]) + Vector3.up * LootPlacementPlanner.AnchorLift;
            _pile.SpawnPile(pieces, points, table);
        }

        private void Save()
        {
            var entries = new List<HaulPileEntry>();
            foreach (GameObject go in _pile.Spawned)
            {
                if (go == null || !go.TryGetComponent(out LootValue value) || value.Item == null)
                    continue;
                entries.Add(new HaulPileEntry(value.Item.name, go.transform.position, go.transform.rotation));
            }
            entries.AddRange(_unresolved);
            HaulPileSave.SaveEntries(SaveSlots.Active, entries);
        }

        /// <summary>The raid's current loot table, which maps each piece back to its prefab.</summary>
        private RaidLootTable RaidTable()
        {
            foreach (LootSpawner spawner in FindObjectsByType<LootSpawner>(FindObjectsSortMode.None))
            {
                if (spawner != _pile && spawner.Table != null)
                    return spawner.Table;
            }
            return null;
        }

        /// <summary>Every loot table reachable: each era's in the catalogue, and the raid spawner's own.</summary>
        private List<RaidLootTable> AllTables()
        {
            var tables = new List<RaidLootTable>();
            RaidDirector director = RaidDirector.Current; // (scene split, #537)
            if (director != null && director.EraContent != null)
            {
                foreach (EraContentCatalogue.Entry entry in director.EraContent.Entries)
                {
                    if (entry != null && entry.Loot != null)
                        tables.Add(entry.Loot);
                }
            }
            RaidLootTable own = RaidTable();
            if (own != null)
                tables.Add(own);
            return tables;
        }

        private LootItem Resolve(string id, out RaidLootTable table)
        {
            foreach (RaidLootTable candidate in AllTables())
            {
                foreach (RaidLootTable.Entry entry in candidate.Entries)
                {
                    if (entry != null && entry.Item != null && entry.Item.name == id)
                    {
                        table = candidate;
                        return entry.Item;
                    }
                }
            }
            table = null;
            return null;
        }
    }
}

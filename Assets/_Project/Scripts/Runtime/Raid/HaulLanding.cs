using System.Collections.Generic;
using Code.Scripts.EventSystems;
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
    /// the raid's loot clear never touches them. See docs/4-systems/raid.md, "How the haul comes home".
    /// </summary>
    public class HaulLanding : MonoBehaviour
    {
        [Tooltip("Spawns and owns the pile. Separate from the raid's spawner so the raid's clear leaves it alone.")]
        [SerializeField] private LootSpawner _pile;

        private bool _restored;
        private int _savedCount = -1;

        // Saved names no loot table could resolve: kept in the save so a missing table never erases a piece.
        private readonly List<string> _unresolved = new List<string>();

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (HaulExtracted e) => Land(e));
            EventManager.Instance?.Subscribe(this, (SaveSlotLoaded e) => Reload());
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void Update()
        {
            if (!_restored)
            {
                if (ServerReady())
                    Restore();
                return;
            }
            // A piece destroyed elsewhere (sold, broken) is no longer in the pile: keep the save in step.
            if (_pile != null && LivePieces().Count != _savedCount)
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
            _savedCount = -1;
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
            var byTable = new Dictionary<RaidLootTable, List<LootItem>>();
            foreach (string id in HaulPileSave.Load(SaveSlots.Active))
            {
                LootItem item = Resolve(id, out RaidLootTable table);
                if (item == null)
                    _unresolved.Add(id);
                else if (byTable.TryGetValue(table, out List<LootItem> items))
                    items.Add(item);
                else
                    byTable[table] = new List<LootItem> { item };
            }
            foreach (KeyValuePair<RaidLootTable, List<LootItem>> group in byTable)
                Spawn(group.Value, group.Key);
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

        private List<LootItem> LivePieces()
        {
            var items = new List<LootItem>();
            foreach (GameObject go in _pile.Spawned)
            {
                if (go != null && go.TryGetComponent(out LootValue value) && value.Item != null)
                    items.Add(value.Item);
            }
            return items;
        }

        private void Save()
        {
            var ids = new List<string>();
            foreach (LootItem item in LivePieces())
                ids.Add(item.name);
            _savedCount = ids.Count;
            ids.AddRange(_unresolved);
            HaulPileSave.Save(SaveSlots.Active, ids.ToArray());
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
            RaidDirector director = FindFirstObjectByType<RaidDirector>();
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

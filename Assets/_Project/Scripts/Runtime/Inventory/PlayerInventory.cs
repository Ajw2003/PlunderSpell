using System.Collections.Generic;
using System.Collections.ObjectModel;
using PurrNet;
using UnityEngine;

namespace RogueAi.Inventory
{
    /// <summary>
    /// Server-authoritative equipped-item set for one player, replicated to all peers.
    ///
    /// PurrNet 1.15 note: PurrNet has no Mirror-style <c>[SyncVar]</c> attribute for collections —
    /// replicated lists use the field-based <see cref="SyncList{T}"/> network module. We keep the
    /// equipped set in a <see cref="SyncList{T}"/>; PurrNet fans out add/remove deltas automatically,
    /// so no manual dirty flag or broadcast RPC is needed. Mutations are server-authoritative and are
    /// funnelled through <see cref="Equip"/>/<see cref="Unequip"/> (call on the server / owner-with-authority).
    ///
    /// Cross-era rule: <see cref="GetEquippedForEra"/> returns the ENTIRE equipped set regardless of the
    /// raid era, because Plunderspell allows loot from any era to be used in any era.
    /// </summary>
    public class PlayerInventory : NetworkBehaviour
    {
        // Field-based SyncList (PurrNet's replicated collection module). Inline-initialised so it is
        // never null. NOTE: ScriptableObject references are resolved by asset GUID at runtime; when
        // shipping, register InventoryItem assets in a networked catalogue / addressables table so the
        // reference survives serialization across peers (see master report "Known manual steps").
        private readonly SyncList<InventoryItem> _equippedItems = new SyncList<InventoryItem>();

        /// <summary>
        /// Read-only view of the currently equipped items. SyncList implements IList&lt;T&gt; but not
        /// IReadOnlyList&lt;T&gt;, so it is wrapped rather than cast (an unwrapped cast compiles but
        /// throws InvalidCastException at runtime).
        /// </summary>
        public IReadOnlyList<InventoryItem> EquippedItems => new ReadOnlyCollection<InventoryItem>(_equippedItems);

        /// <summary>Number of equipped items.</summary>
        public int EquippedCount => _equippedItems.Count;

        /// <summary>
        /// Equip an item: adds it to the replicated set. PurrNet broadcasts the add delta to all
        /// clients automatically. No-ops if the item is null or already equipped.
        /// </summary>
        public void Equip(InventoryItem item)
        {
            if (item == null || _equippedItems.Contains(item))
                return;

            _equippedItems.Add(item);
        }

        /// <summary>Unequip an item: removes it from the replicated set.</summary>
        public void Unequip(InventoryItem item)
        {
            if (item == null)
                return;

            _equippedItems.Remove(item);
        }

        /// <summary>
        /// Returns every equipped item — cross-era equipping is always allowed, so the raid era is
        /// accepted only for API symmetry and does NOT filter the result.
        /// </summary>
        public List<InventoryItem> GetEquippedForEra(HistoricalEra era)
        {
            var result = new List<InventoryItem>(_equippedItems.Count);
            foreach (var item in _equippedItems)
            {
                // CanEquipInEra always returns true by design; kept explicit to document intent.
                if (item != null && item.CanEquipInEra(era))
                    result.Add(item);
            }
            return result;
        }
    }
}

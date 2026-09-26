using UnityEngine;

namespace Plunderspell.Inventory
{
    /// <summary>Broad functional bucket for an inventory item.</summary>
    public enum ItemCategory
    {
        Weapon,
        Armour,
        Tool,
        Consumable
    }

    /// <summary>
    /// Design-time data for a single item a player can own and equip. Immutable-by-convention:
    /// <see cref="EraAcquired"/> is set once at creation and represents the era the item was
    /// looted from. It has NO bearing on where the item may be used — Plunderspell deliberately
    /// allows cross-era equipping (an Age-of-Powder musket may be carried into a Bronze-Age raid),
    /// so <see cref="CanEquipInEra"/> always returns true.
    /// </summary>
    [CreateAssetMenu(fileName = "InventoryItem", menuName = "Plunderspell/Inventory Item")]
    public class InventoryItem : ScriptableObject
    {
        [Header("Identity")]
        public string ItemName = "Unnamed Item";

        [TextArea]
        public string Description = "";

        [Header("Provenance")]
        [Tooltip("Era this item was acquired from. Set once at creation; does not restrict use.")]
        public HistoricalEra EraAcquired = HistoricalEra.BronzeAge;

        [Header("Classification")]
        public ItemCategory Category = ItemCategory.Weapon;

        [Tooltip("Weight in stone — used by carry/encumbrance systems.")]
        public float Weight = 1.0f;

        /// <summary>
        /// Whether this item may be equipped in a raid set in <paramref name="raidEra"/>.
        /// By deliberate design there is NO era restriction: always true.
        /// </summary>
        public bool CanEquipInEra(HistoricalEra raidEra) => true;
    }
}

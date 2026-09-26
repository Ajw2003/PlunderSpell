using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// Immutable design-time data describing a single lootable treasure. This is a pure data bag
    /// (ScriptableObject) — the runtime physics/networking behaviour lives on <see cref="LootPickup"/>.
    ///
    /// The economy/heist loop reads three physical properties off this asset:
    /// <list type="bullet">
    /// <item><see cref="Worth"/> — coin value awarded on a clean extraction.</item>
    /// <item><see cref="Bulk"/> — weight in "stone"; anything over 10 stone requires a two-player carry.</item>
    /// <item><see cref="Fragility"/> — the collision relative-velocity (m/s) above which the item shatters.</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(fileName = "LootItem", menuName = "Plunderspell/Loot/Loot Item", order = 0)]
    public class LootItem : ScriptableObject
    {
        [Header("Economy")]
        [Tooltip("Coin value awarded when this item is extracted intact.")]
        public float Worth = 50f;

        [Tooltip("Artifact-tier loot pays a bonus on a clean (unbroken) extraction.")]
        public bool IsArtifact = false;

        [Header("Physics")]
        [Tooltip("Weight in stone. Bulk > 10 forces a two-player dual carry.")]
        public float Bulk = 3.5f;

        [Tooltip("Collision relative-velocity threshold (m/s). An impact whose magnitude exceeds " +
                 "this value shatters the item. Use float.MaxValue / 999 for unbreakable loot.")]
        public float Fragility = 8.0f;

        [Header("Presentation")]
        [Tooltip("Human-readable name shown in the carry HUD and loot log.")]
        public string DisplayName = "Unnamed Loot";

        [Tooltip("Icon shown in the inventory / extraction summary.")]
        public Sprite Icon;

        /// <summary>Bulk threshold (in stone) above which an item cannot be carried by one player.</summary>
        public const float DualCarryBulkThreshold = 10f;

        /// <summary>True when this item is heavy enough to demand two carriers.</summary>
        public bool RequiresDualCarry => Bulk > DualCarryBulkThreshold;
    }
}

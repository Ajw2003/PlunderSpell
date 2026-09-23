using UnityEngine;

namespace RogueAi.Loot
{
    /// <summary>
    /// What a carryable object is worth when it leaves the castle. Sits alongside <c>Item</c> on
    /// loot prefabs; anything without one extracts for nothing.
    ///
    /// See docs/systems/raid.md, "Carrying and extracting".
    /// </summary>
    public class LootValue : MonoBehaviour
    {
        [Tooltip("Authored loot definition. When set, it is the source of truth for worth and name.")]
        [SerializeField] private LootItem m_item;

        [Tooltip("Worth used when no loot definition is assigned.")]
        [SerializeField] private float m_worth = 50f;

        [Tooltip("Name used when no loot definition is assigned.")]
        [SerializeField] private string m_displayName = "Loot";

        [Tooltip("Set while the piece is ruined, so a smashed goblet tallies as nothing.")]
        [SerializeField] private bool m_isRuined;

        /// <summary>Gold this is worth at extraction. Zero once ruined.</summary>
        public float Worth => m_isRuined ? 0f : (m_item != null ? m_item.Worth : m_worth);

        /// <summary>Name to show in the HUD.</summary>
        public string DisplayName => m_item != null ? m_item.DisplayName : m_displayName;

        /// <summary>True once this piece has been broken and is no longer worth carrying out.</summary>
        public bool IsRuined => m_isRuined;

        /// <summary>The authored definition behind this piece, when it has one.</summary>
        public LootItem Item => m_item;

        /// <summary>Marks the piece ruined, dropping its worth to zero.</summary>
        /// <summary>Raised the moment a piece is ruined: (the piece, the worth it just lost).</summary>
        public static event System.Action<LootValue, float> Ruined;

        public void Ruin()
        {
            if (m_isRuined)
                return;
            float lost = Worth;
            m_isRuined = true;
            Ruined?.Invoke(this, lost);
        }

        /// <summary>Editor/tooling seam: point this at an authored definition.</summary>
        public void SetItem(LootItem item) => m_item = item;
    }
}

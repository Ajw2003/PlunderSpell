using UnityEngine;

namespace Plunderspell.Loot
{
    /// <summary>
    /// Lights a lootable object, or a door, in verdigris while the player is looking at it and in reach, so "you can use
    /// this" is something the world says rather than a line of text (#318).
    ///
    /// Why a property block rather than an outline shader, and why this is attached at runtime:
    /// docs/6-decisions/Decisions.md, "Focus glow is a property block, added at runtime".
    /// </summary>
    public class LootHighlight : MonoBehaviour
    {
        [Tooltip("Renderers that glow. Filled from this object's children when left empty.")]
        [SerializeField] private Renderer[] m_renderers;

        [Tooltip("Emissive colour added while focused: the UI's verdigris, the colour of 'you can touch this'.")]
        [SerializeField] private Color m_glow = new Color(0.37f, 0.64f, 0.53f);

        [Tooltip("Strength of the added emission.")]
        [SerializeField] private float m_intensity = 1.8f;

        private static readonly int s_emissionColor = Shader.PropertyToID("_EmissionColor");

        private MaterialPropertyBlock m_block;
        private bool m_highlighted;

        /// <summary>True while this item is being glowed at.</summary>
        public bool IsHighlighted => m_highlighted;

        private void Awake()
        {
            if (m_renderers == null || m_renderers.Length == 0)
                m_renderers = GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>Switches the glow on or off. Driven by <see cref="LootInteractor"/>'s focus.</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (m_highlighted == highlighted)
                return;

            m_highlighted = highlighted;
            Apply();
        }

        private void Apply()
        {
            if (m_renderers == null)
                return;

            m_block ??= new MaterialPropertyBlock();
            Color emission = m_highlighted ? m_glow * m_intensity : Color.black;

            for (int i = 0; i < m_renderers.Length; i++)
            {
                Renderer renderer = m_renderers[i];
                if (renderer == null)
                    continue;

                renderer.GetPropertyBlock(m_block);
                m_block.SetColor(s_emissionColor, emission);
                renderer.SetPropertyBlock(m_block);
            }
        }
    }
}

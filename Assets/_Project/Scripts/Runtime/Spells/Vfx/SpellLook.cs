using UnityEngine;

namespace RogueAi.Spells.Vfx
{
    /// <summary>How a spell shows itself.</summary>
    public enum SpellVisualStyle
    {
        /// <summary>A shell of light at the caster's hands.</summary>
        Burst = 0,

        /// <summary>A bolt that flies where the caster was aiming.</summary>
        Bolt = 1,
    }

    /// <summary>The colour and shape one spell reads as.</summary>
    public readonly struct SpellLook
    {
        public readonly Color Colour;
        public readonly SpellVisualStyle Style;
        public readonly float Radius;

        public SpellLook(Color colour, SpellVisualStyle style = SpellVisualStyle.Burst,
            float radius = 2.5f)
        {
            Colour = colour;
            Style = style;
            Radius = radius;
        }
    }

    /// <summary>
    /// What each spell looks like. A misfire is always the same angry orange whatever it was meant
    /// to be, so "that went wrong" is readable before the caption is.
    ///
    /// See docs/systems/spells.md, "Seeing a cast".
    /// </summary>
    public static class SpellLookbook
    {
        /// <summary>Every misfire reads as this.</summary>
        public static readonly SpellLook Misfire =
            new SpellLook(new Color(1f, 0.35f, 0.15f), SpellVisualStyle.Burst, 3f);

        /// <summary>The look for <paramref name="spell"/>, or a plain white burst if it has none.</summary>
        public static SpellLook For(SpellId spell)
        {
            if (SpellCatalogue.IsMisfire(spell))
            {
                return Misfire;
            }

            switch (spell)
            {
                case SpellId.Ignis:
                    return new SpellLook(new Color(1f, 0.45f, 0.1f), SpellVisualStyle.Bolt, 0.6f);
                case SpellId.Frango:
                    return new SpellLook(new Color(0.85f, 0.9f, 1f), SpellVisualStyle.Burst, 2.2f);
                case SpellId.Levo:
                    return new SpellLook(new Color(0.6f, 0.45f, 1f), SpellVisualStyle.Burst, 2f);
                case SpellId.AurumVoco:
                    return new SpellLook(new Color(1f, 0.85f, 0.25f), SpellVisualStyle.Burst, 2.4f);
                case SpellId.Tonitrus:
                    return new SpellLook(new Color(0.75f, 0.85f, 1f), SpellVisualStyle.Burst, 4f);
                case SpellId.Somnus:
                    return new SpellLook(new Color(0.35f, 0.55f, 0.85f), SpellVisualStyle.Burst, 2.6f);
                case SpellId.CadaverSurge:
                    return new SpellLook(new Color(0.45f, 0.75f, 0.4f), SpellVisualStyle.Burst, 2.4f);
                case SpellId.Porta:
                    return new SpellLook(new Color(0.55f, 0.8f, 1f), SpellVisualStyle.Burst, 1.8f);
                default:
                    return new SpellLook(Color.white);
            }
        }
    }
}

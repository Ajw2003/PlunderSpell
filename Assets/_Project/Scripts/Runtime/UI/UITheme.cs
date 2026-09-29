using System.Text;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// The pigment list and the roles they play, so a screen says what a colour is for rather than
    /// which colour it is. The pigments are the design system's (docs/generated/design-system/project/
    /// tokens.json). Each role has one meaning and must not be borrowed for another: Interactive is only
    /// for things you can use or select, Value only for gold and loot, Danger only for health, alarm,
    /// the last minute and death, Voice only for mana, the microphone and heard speech.
    /// </summary>
    public static class UITheme
    {
        // --- Pigments -----------------------------------------------------------------------------
        public static readonly Color BoneBlack = new Color32(0x14, 0x12, 0x0E, 255);
        public static readonly Color Ash = new Color32(0x1E, 0x1A, 0x14, 255);
        public static readonly Color AshHi = new Color32(0x28, 0x23, 0x18, 255);
        public static readonly Color Vellum = new Color32(0xDC, 0xD2, 0xBA, 255);
        public static readonly Color VellumDim = new Color32(0x9A, 0x90, 0x78, 255);
        public static readonly Color VellumFaint = new Color32(0x63, 0x5C, 0x4C, 255);
        public static readonly Color Verdigris = new Color32(0x5F, 0xA2, 0x88, 255);
        public static readonly Color VerdigrisLo = new Color32(0x2E, 0x4C, 0x41, 255);
        public static readonly Color Orpiment = new Color32(0xC9, 0xA2, 0x27, 255);
        public static readonly Color Madder = new Color32(0xC4, 0x54, 0x2E, 255);
        public static readonly Color MadderLo = new Color32(0x5E, 0x2A, 0x18, 255);
        public static readonly Color Lapis = new Color32(0x7A, 0x6A, 0xA0, 255);
        public static readonly Color LapisLo = new Color32(0x3A, 0x33, 0x50, 255);
        public static readonly Color Flint = new Color32(0x7A, 0x75, 0x68, 255);
        public static readonly Color Umber = new Color32(0x5B, 0x44, 0x30, 255);
        public static readonly Color Line = new Color32(0x33, 0x2D, 0x22, 255);
        public static readonly Color LineSoft = new Color32(0x26, 0x21, 0x19, 255);

        // --- Roles --------------------------------------------------------------------------------
        public static readonly Color Ground = BoneBlack;
        public static readonly Color Surface = Ash;
        public static readonly Color SurfaceHi = AshHi;
        public static readonly Color Text = Vellum;
        public static readonly Color TextDim = VellumDim;
        public static readonly Color TextFaint = VellumFaint;
        public static readonly Color Interactive = Verdigris;
        public static readonly Color InteractiveLo = VerdigrisLo;
        public static readonly Color Value = Orpiment;
        public static readonly Color Danger = Madder;
        public static readonly Color DangerLo = MadderLo;
        public static readonly Color Voice = Lapis;
        public static readonly Color VoiceLo = LapisLo;

        // --- Type scale, in canvas units at the 1920x1080 reference ---------------------------------
        public const int Hero = 156;
        public const int Title = 92;
        public const int Heading = 64;
        public const int Subheading = 38;
        public const int Button = 27;
        public const int Body = 23;
        public const int Label = 15;
        public const int Small = 13;

        /// <summary>Thin space: legacy Text has no letter-spacing, and every UI font has this glyph.</summary>
        private const char ThinSpace = ' ';

        /// <summary>
        /// The alarm as one ramp from quiet to madder: Calm is faint vellum, Stirred the dark madder,
        /// Roused and Hue and cry full madder (the HUD pulses the last).
        /// </summary>
        public static Color AlarmColour(AlarmState state)
        {
            switch (state)
            {
                case AlarmState.Stirred: return DangerLo;
                case AlarmState.Roused: return Danger;
                case AlarmState.HueAndCry: return Danger;
                default: return TextFaint;
            }
        }

        /// <summary>Upper-cases the text and puts a thin space between every character, the eyebrow look.</summary>
        public static string Tracked(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            string upper = text.ToUpperInvariant();
            var builder = new StringBuilder(upper.Length * 2);
            for (int i = 0; i < upper.Length; i++)
            {
                if (i > 0)
                    builder.Append(ThinSpace);
                builder.Append(upper[i]);
            }
            return builder.ToString();
        }
    }
}

using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// The design system's three typefaces (Eczar, Spectral, Overpass Mono), loaded once from
    /// Resources/UI/Fonts. A font that is missing logs one warning and falls back to Unity's built-in
    /// font, so the game never shows no text at all.
    /// </summary>
    public static class UIFonts
    {
        private const string Folder = "UI/Fonts/";

        private sealed class Slot
        {
            public readonly string File;
            public Font Font;

            public Slot(string file) => File = file;
        }

        private static readonly Slot DisplaySlot = new Slot("Eczar-SemiBold");
        private static readonly Slot DisplayHeavySlot = new Slot("Eczar-ExtraBold");
        private static readonly Slot BodySlot = new Slot("Spectral-Regular");
        private static readonly Slot BodyLightSlot = new Slot("Spectral-Light");
        private static readonly Slot BodyItalicSlot = new Slot("Spectral-Italic");
        private static readonly Slot MonoSlot = new Slot("OverpassMono-Medium");
        private static readonly Slot MonoBoldSlot = new Slot("OverpassMono-SemiBold");

        /// <summary>Titles, the raid clock, spell words, big figures.</summary>
        public static Font Display => Get(DisplaySlot);

        /// <summary>The wordmark and the largest figures.</summary>
        public static Font DisplayHeavy => Get(DisplayHeavySlot);

        /// <summary>Sentences.</summary>
        public static Font Body => Get(BodySlot);

        public static Font BodyLight => Get(BodyLightSlot);

        /// <summary>Captions of what you said, and small notes.</summary>
        public static Font BodyItalic => Get(BodyItalicSlot);

        /// <summary>Eyebrows, keys, costs, and any number in a row.</summary>
        public static Font Mono => Get(MonoSlot);

        public static Font MonoBold => Get(MonoBoldSlot);

        private static Font Get(Slot slot)
        {
            // A destroyed font (leaving play mode with domain reload off) compares equal to null, so
            // it loads again rather than drawing nothing.
            if (slot.Font != null)
                return slot.Font;

            string path = Folder + slot.File;
            Font font = Resources.Load<Font>(path);
            if (font == null)
            {
                Debug.LogWarning($"UIFonts: could not load Resources/{path}; using LegacyRuntime.ttf instead.");
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            slot.Font = font;
            return font;
        }
    }
}

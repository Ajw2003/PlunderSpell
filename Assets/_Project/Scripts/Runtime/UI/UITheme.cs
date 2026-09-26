using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>Shared palette and type scale so every screen reads as one system.</summary>
    public static class UITheme
    {
        public static readonly Color Background = new Color32(18, 22, 30, 235);
        public static readonly Color PanelBackground = new Color32(28, 34, 46, 245);
        public static readonly Color Accent = new Color32(198, 156, 74, 255);
        public static readonly Color AccentDark = new Color32(140, 106, 46, 255);
        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color32(190, 196, 208, 255);
        public static readonly Color Danger = new Color32(196, 70, 70, 255);
        public static readonly Color Success = new Color32(90, 182, 110, 255);
        public static readonly Color ManaColor = new Color32(84, 140, 214, 255);

        public const int TitleFontSize = 48;
        public const int HeaderFontSize = 28;
        public const int BodyFontSize = 20;
        public const int SmallFontSize = 16;
    }
}

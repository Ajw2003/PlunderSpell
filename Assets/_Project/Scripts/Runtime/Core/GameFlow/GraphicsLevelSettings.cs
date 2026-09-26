namespace Plunderspell.Core
{
    /// <summary>
    /// Where the player's graphics level is saved. Shared by the Settings screen (which writes it)
    /// and the atmosphere (which applies it at startup, or picks a default for the machine), neither
    /// of which can reference the other.
    /// </summary>
    public static class GraphicsLevelSettings
    {
        /// <summary>PlayerPrefs key holding the chosen quality level's name ("Low", "Medium", "High"); unset means pick for the machine.</summary>
        public const string QualityKey = "Settings.GraphicsQuality";
    }
}

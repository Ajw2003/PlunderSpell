using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// The playtest sound filter. What plays is set in
    /// <c>Assets/_Project/Resources/SoundFocusSettings.asset</c> (see <see cref="SoundFocusSettings"/>);
    /// select it and tick or untick groups in the Inspector. If the asset is missing, everything plays.
    /// </summary>
    public static class SoundFocus
    {
        public const string SettingsResource = "SoundFocusSettings";

        private static SoundFocusSettings s_settings;
        private static bool s_reportedMissing;
        private static bool? s_enabledOverride;

        /// <summary>The settings asset, loaded once from Resources; null if it is missing.</summary>
        public static SoundFocusSettings Settings
        {
            get
            {
                if (s_settings == null)
                {
                    s_settings = Resources.Load<SoundFocusSettings>(SettingsResource);
                    if (s_settings == null && !s_reportedMissing)
                    {
                        s_reportedMissing = true;
                        Debug.LogWarning($"[Audio] No Resources/{SettingsResource}.asset; every sound plays.");
                    }
                }
                return s_settings;
            }
        }

        /// <summary>
        /// True while the filter applies: the asset's "Filter On", unless code set this (tests and the
        /// latency probe switch it off for a run without touching the asset).
        /// </summary>
        public static bool Enabled
        {
            get => s_enabledOverride ?? (Settings != null && Settings.FilterOn);
            set => s_enabledOverride = value;
        }

        /// <summary>Drops a value set through <see cref="Enabled"/>, so the asset decides again.</summary>
        public static void ClearOverride() => s_enabledOverride = null;

        /// <summary>Whether <paramref name="soundName"/> may play right now.</summary>
        public static bool Allows(string soundName)
        {
            if (!Enabled || Settings == null)
                return true;
            return Settings.Allows(soundName);
        }
    }
}

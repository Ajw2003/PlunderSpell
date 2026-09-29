using System;

namespace Plunderspell.Audio
{
    /// <summary>
    /// A reduced sound set for playtesting: with it on, only footsteps and movement foley, physics
    /// (impacts, breaking, scraping), guards (voices, hounds, weapons and hits) and UI clicks play.
    /// Spells, music, ambience, stingers, doors and the rest stay in the SoundBank, unused, so the
    /// game is quiet enough to tell those apart. Set <see cref="Enabled"/> false to hear everything.
    /// </summary>
    public static class SoundFocus
    {
        /// <summary>True while only the reduced set plays.</summary>
        public static bool Enabled = true;

        private static readonly string[] s_kept =
        {
            "foley_",     // footsteps, jump and land, gear rattle on each step
            "phys_",      // impacts, breaking, scraping
            "vo_",        // guard and hound voices
            "sfx_enemy",  // guard sounds
            "sfx_wpn",    // guard swings, bolts, hits
            "ui_",        // menu clicks
        };

        /// <summary>Whether <paramref name="soundName"/> may play right now.</summary>
        public static bool Allows(string soundName)
        {
            if (!Enabled)
                return true;
            if (string.IsNullOrEmpty(soundName))
                return false;
            foreach (string prefix in s_kept)
            {
                if (soundName.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}

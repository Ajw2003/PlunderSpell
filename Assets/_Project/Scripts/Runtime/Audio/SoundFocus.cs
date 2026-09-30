using System;

namespace Plunderspell.Audio
{
    /// <summary>
    /// A reduced sound set for playtesting: with it on, only footsteps and movement foley, guards
    /// (voices, hounds, weapons and hits), UI clicks and the replaced spells play. Physics (impacts, breaking,
    /// scraping: the tone read wrong, e.g. leather on stone sounded like metal), music, ambience,
    /// stingers, doors and the rest stay in the SoundBank, unused, so the game is quiet enough to
    /// tell those apart. Set <see cref="Enabled"/> false to hear everything.
    /// </summary>
    public static class SoundFocus
    {
        /// <summary>True while only the reduced set plays.</summary>
        public static bool Enabled = true;

        private static readonly string[] s_kept =
        {
            "foley_",     // footsteps, jump and land, gear rattle on each step
            "vo_",        // guard and hound voices
            "sfx_enemy",  // guard sounds
            "sfx_wpn",    // guard swings, bolts, hits
            "ui_",        // menu clicks
            "sfx_spell_", // spells, replaced from the owner's picks 2026-09-30
        };

        // Spell sounds still waiting on a replacement; they keep the old sound, so they stay muted.
        private static readonly string[] s_pending =
        {
            "sfx_spell_ignis_cast",
            "sfx_spell_ignis_travel_loop",
            "sfx_spell_ignis_impact",
            "sfx_spell_levo_release",
            "sfx_spell_levo_misfire",
        };

        /// <summary>Whether <paramref name="soundName"/> may play right now.</summary>
        public static bool Allows(string soundName)
        {
            if (!Enabled)
                return true;
            if (string.IsNullOrEmpty(soundName))
                return false;
            foreach (string pending in s_pending)
            {
                if (soundName == pending)
                    return false;
            }
            foreach (string prefix in s_kept)
            {
                if (soundName.StartsWith(prefix, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}

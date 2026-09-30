using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// Which sounds play, edited in the Inspector: select
    /// <c>Assets/_Project/Resources/SoundFocusSettings.asset</c>. Changes take effect at once, in Play
    /// mode too. A sound plays when its own override says so; otherwise the group with the longest
    /// matching prefix decides; otherwise "Play everything else". Untick "Filter on" to hear everything.
    /// Read through <see cref="SoundFocus"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Plunderspell/Audio/Sound Focus Settings", fileName = "SoundFocusSettings")]
    public sealed class SoundFocusSettings : ScriptableObject
    {
        /// <summary>A named set of sounds, picked by name prefix, switched on or off together.</summary>
        [Serializable]
        public sealed class Group
        {
            public string Label;
            public bool Play = true;
            [Tooltip("Sound names starting with any of these belong to the group, e.g. \"sfx_spell_\".")]
            public string[] Prefixes = Array.Empty<string>();

            public Group() { }

            public Group(string label, bool play, params string[] prefixes)
            {
                Label = label;
                Play = play;
                Prefixes = prefixes;
            }
        }

        /// <summary>One sound by its exact SoundBank name, e.g. "sfx_spell_ignis_impact".</summary>
        [Serializable]
        public sealed class Override
        {
            public string Sound;
            public bool Play;

            public Override() { }

            public Override(string sound, bool play)
            {
                Sound = sound;
                Play = play;
            }
        }

        [Tooltip("Off: every sound plays and the lists below are ignored.")]
        public bool FilterOn = true;

        [Tooltip("For a sound no group or override names.")]
        public bool PlayEverythingElse;

        public List<Group> Groups = new List<Group>
        {
            new Group("Footsteps and movement", true, "foley_"),
            new Group("Guard and hound voices", true, "vo_"),
            new Group("Guard sounds", true, "sfx_enemy"),
            new Group("Weapons: swings, bolts, hits", true, "sfx_wpn"),
            new Group("Menu and UI", true, "ui_"),
            new Group("Spells", true, "sfx_spell_"),
            new Group("Physics: impacts, breaks, scrapes", false, "phys_"),
            new Group("Music", false, "mus_"),
            new Group("Ambience", false, "amb_"),
            new Group("Stingers", false, "sting_"),
            new Group("Castle: doors, gates, torches", false,
                "sfx_door", "sfx_portcullis", "sfx_drawbridge", "sfx_castle", "sfx_masonry", "sfx_torch"),
            new Group("Player: hurt, grab, throw, carry, voice", false,
                "sfx_player", "sfx_grab", "sfx_throw", "sfx_carry", "sfx_voice"),
            new Group("Portal, loot, extraction, results, lair", false,
                "sfx_portal", "sfx_extract", "sfx_loot", "sfx_result", "sfx_lair"),
            new Group("Hazards, fire, status, debris", false, "sfx_hazard", "sfx_fire", "sfx_status", "sfx_debris"),
        };

        [Tooltip("Exact sound names that ignore their group.")]
        public List<Override> Overrides = new List<Override>
        {
            new Override("sfx_spell_ignis_cast", false),
            new Override("sfx_spell_ignis_travel_loop", false),
            new Override("sfx_spell_ignis_impact", false),
            new Override("sfx_spell_levo_release", false),
            new Override("sfx_spell_levo_misfire", false),
        };

        /// <summary>Whether <paramref name="soundName"/> plays under these settings, filter on.</summary>
        public bool Allows(string soundName)
        {
            if (string.IsNullOrEmpty(soundName))
                return false;

            foreach (Override entry in Overrides)
            {
                if (entry != null && entry.Sound == soundName)
                    return entry.Play;
            }

            Group best = null;
            int bestLength = -1;
            foreach (Group group in Groups)
            {
                if (group?.Prefixes == null)
                    continue;
                foreach (string prefix in group.Prefixes)
                {
                    if (!string.IsNullOrEmpty(prefix) && prefix.Length > bestLength
                        && soundName.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        best = group;
                        bestLength = prefix.Length;
                    }
                }
            }
            return best != null ? best.Play : PlayEverythingElse;
        }
    }
}

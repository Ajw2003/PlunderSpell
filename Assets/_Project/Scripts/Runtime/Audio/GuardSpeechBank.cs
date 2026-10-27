using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Audio
{
    /// <summary>
    /// The recorded guard lines (Resources/GuardVoice, built by Tools/GuardVoice/sync_base_to_game.py),
    /// indexed by Age and situation from their file names: vo_&lt;age&gt;_base_&lt;situation&gt;_&lt;NN&gt;.
    /// </summary>
    public sealed class GuardSpeechBank
    {
        public const string ResourceFolder = "GuardVoice";

        // A guard is never silent: an Age with no clip for a line borrows the next Age in this order.
        private static readonly string[] FallbackAges = { "powder", "high", "late", "bronze" };

        private static GuardSpeechBank s_shared;

        private readonly Dictionary<string, List<AudioClip>> _bySituation = new Dictionary<string, List<AudioClip>>();

        public GuardSpeechBank(IEnumerable<AudioClip> clips)
        {
            foreach (AudioClip clip in clips)
            {
                if (clip != null && TryParse(clip.name, out string age, out string situation))
                    ListFor(age, situation, create: true).Add(clip);
            }
        }

        /// <summary>The bank of every clip in Resources, loaded on first use.</summary>
        public static GuardSpeechBank Shared =>
            s_shared ?? (s_shared = new GuardSpeechBank(Resources.LoadAll<AudioClip>(ResourceFolder)));

        public int ClipCount
        {
            get
            {
                int count = 0;
                foreach (List<AudioClip> list in _bySituation.Values)
                    count += list.Count;
                return count;
            }
        }

        public static string Situation(GuardLine line)
        {
            switch (line)
            {
                case GuardLine.Murmur: return "murmur";
                case GuardLine.Alert: return "alert";
                case GuardLine.Chase: return "chase";
                case GuardLine.Search: return "search";
                case GuardLine.Lost: return "lost";
                case GuardLine.Attack: return "attack";
                case GuardLine.Hurt: return "hurt";
                case GuardLine.Death: return "death";
                default: return "asleep";
            }
        }

        /// <summary>A random clip of that Age and line; failing that, the same line from the fallback Ages.</summary>
        public bool TryPick(string age, GuardLine line, System.Random rng, out AudioClip clip)
        {
            string situation = Situation(line);
            if (TryPickFrom(age, situation, rng, out clip))
                return true;
            foreach (string fallback in FallbackAges)
            {
                if (fallback != age && TryPickFrom(fallback, situation, rng, out clip))
                    return true;
            }
            clip = null;
            return false;
        }

        private bool TryPickFrom(string age, string situation, System.Random rng, out AudioClip clip)
        {
            List<AudioClip> list = age == null ? null : ListFor(age, situation, create: false);
            if (list == null || list.Count == 0)
            {
                clip = null;
                return false;
            }
            clip = list[rng.Next(list.Count)];
            return true;
        }

        private List<AudioClip> ListFor(string age, string situation, bool create)
        {
            string key = age + "/" + situation;
            if (_bySituation.TryGetValue(key, out List<AudioClip> list))
                return list;
            if (!create)
                return null;
            list = new List<AudioClip>();
            _bySituation[key] = list;
            return list;
        }

        private static bool TryParse(string clipName, out string age, out string situation)
        {
            age = situation = null;
            string[] parts = clipName.Split('_');
            if (parts.Length != 5 || parts[0] != "vo" || parts[2] != "base")
                return false;
            age = parts[1];
            situation = parts[3];
            return true;
        }
    }
}

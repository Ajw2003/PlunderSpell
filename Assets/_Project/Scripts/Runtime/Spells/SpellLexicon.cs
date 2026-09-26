using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Spells
{
    /// <summary>
    /// The full spoken-word spellbook. Holds every authored <see cref="SpellWord"/> and
    /// provides exact / fuzzy lookups used by <see cref="MisfireEngine"/> to turn a
    /// recognised phrase into a <see cref="SpellId"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellLexicon", menuName = "Plunderspell/Spell Lexicon")]
    public class SpellLexicon : ScriptableObject
    {
        [Tooltip("All spell-word entries. Exact-match trigger words plus their misfire near-matches.")]
        public List<SpellWord> Spells = new List<SpellWord>();

        [Tooltip("Maximum Levenshtein distance for a near-match (misfire) to be accepted.")]
        public int MaxNearMatchDistance = 2;

        /// <summary>
        /// The speech recogniser's vocabulary: every heard-as spelling mapped to the word the
        /// lexicon matches. A correct spelling maps to <see cref="SpellWord.Word"/>; a misfire
        /// spelling maps to the first authored mispronunciation, so it resolves as a misfire.
        /// </summary>
        public Dictionary<string, string> BuildHeardVocabulary()
        {
            var vocabulary = new Dictionary<string, string>();
            if (Spells == null)
                return vocabulary;

            foreach (SpellWord sw in Spells)
            {
                if (sw == null || string.IsNullOrEmpty(sw.Word))
                    continue;

                AddHeard(vocabulary, sw.HeardAs, sw.Word);

                string misfireWord = sw.AltPronunciations != null && sw.AltPronunciations.Length > 0
                    ? sw.AltPronunciations[0].ToUpperInvariant()
                    : null;
                if (misfireWord != null)
                    AddHeard(vocabulary, sw.MisfireHeardAs, misfireWord);
            }
            return vocabulary;
        }

        private static void AddHeard(Dictionary<string, string> vocabulary, string[] heard, string word)
        {
            if (heard == null)
                return;
            foreach (string h in heard)
            {
                if (!string.IsNullOrWhiteSpace(h))
                    vocabulary[h.Trim().ToLowerInvariant()] = word;
            }
        }

        /// <summary>
        /// Mana a resolved cast costs: the <see cref="SpellWord.ManaCost"/> of the word whose spell
        /// or misfire it is. 0 for <see cref="SpellId.None"/> or a spell no word in this lexicon owns.
        /// </summary>
        public int ManaCostOf(SpellId resolved)
        {
            if (resolved == SpellId.None || Spells == null)
                return 0;

            for (int i = 0; i < Spells.Count; i++)
            {
                SpellWord sw = Spells[i];
                if (sw != null && (sw.spellId == resolved || sw.misfireId == resolved))
                    return sw.ManaCost;
            }
            return 0;
        }

        /// <summary>Exact match on the normalised trigger word. Null if none.</summary>
        public SpellWord FindByWord(string normalized)
        {
            if (string.IsNullOrEmpty(normalized) || Spells == null)
                return null;

            for (int i = 0; i < Spells.Count; i++)
            {
                var sw = Spells[i];
                if (sw == null || string.IsNullOrEmpty(sw.Word))
                    continue;
                if (string.Equals(sw.Word, normalized, System.StringComparison.Ordinal))
                    return sw;
            }
            return null;
        }

        /// <summary>
        /// Best fuzzy match within <see cref="MaxNearMatchDistance"/> Levenshtein distance.
        /// Considers both the primary trigger word and each authored alt-pronunciation, and
        /// returns the <see cref="SpellWord"/> with the smallest distance (ties resolved by
        /// list order). Null if nothing is close enough.
        /// </summary>
        public SpellWord FindByNearMatch(string normalized)
        {
            if (string.IsNullOrEmpty(normalized) || Spells == null)
                return null;

            SpellWord best = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < Spells.Count; i++)
            {
                var sw = Spells[i];
                if (sw == null)
                    continue;

                int distance = int.MaxValue;

                if (!string.IsNullOrEmpty(sw.Word))
                    distance = MisfireEngine.LevenshteinDistance(normalized, sw.Word);

                if (sw.AltPronunciations != null)
                {
                    for (int a = 0; a < sw.AltPronunciations.Length; a++)
                    {
                        string alt = sw.AltPronunciations[a];
                        if (string.IsNullOrEmpty(alt))
                            continue;
                        int d = MisfireEngine.LevenshteinDistance(normalized, alt.ToUpperInvariant());
                        if (d < distance)
                            distance = d;
                    }
                }

                if (distance <= MaxNearMatchDistance && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = sw;
                    if (bestDistance == 0)
                        break;
                }
            }

            return best;
        }
    }
}

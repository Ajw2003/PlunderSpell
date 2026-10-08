using System.Collections.Generic;

namespace Plunderspell.Market
{
    /// <summary>
    /// The haggling words as the speech recogniser hears them. Plain C#, so the voice vocabulary and the tests share it.
    /// The recogniser reports the canonical word (PLUS, SATIS, VALE); <see cref="TryParse"/> turns that back into a <see cref="HaggleWord"/>.
    /// </summary>
    public static class HaggleWords
    {
        // English spellings the small model may output for each Latin word, lower case (the vocabulary's heard side).
        private static readonly Dictionary<string, HaggleWord> Heard = new Dictionary<string, HaggleWord>
        {
            { "plus", HaggleWord.Plus },
            { "satis", HaggleWord.Satis },
            { "sat is", HaggleWord.Satis },
            { "sad is", HaggleWord.Satis },
            { "vale", HaggleWord.Vale },
            { "valley", HaggleWord.Vale },
            { "veil", HaggleWord.Vale },
        };

        /// <summary>Adds each heard spelling -> canonical word to a recogniser vocabulary, never replacing a spell's entry.</summary>
        public static void AddTo(IDictionary<string, string> vocabulary)
        {
            foreach (KeyValuePair<string, HaggleWord> pair in Heard)
            {
                if (!vocabulary.ContainsKey(pair.Key))
                    vocabulary.Add(pair.Key, pair.Value.ToString().ToUpperInvariant());
            }
        }

        /// <summary>True when the recogniser's normalised text is a haggling word (canonical or a heard spelling).</summary>
        public static bool TryParse(string normalizedText, out HaggleWord word)
        {
            word = default;
            return !string.IsNullOrEmpty(normalizedText) && Heard.TryGetValue(normalizedText.Trim().ToLowerInvariant(), out word);
        }
    }
}

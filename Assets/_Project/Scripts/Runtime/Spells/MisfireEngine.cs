using RogueAi.Voice;

namespace RogueAi.Spells
{
    /// <summary>
    /// Pure resolution logic that turns a recognised phrase into the spell that actually
    /// fires. This is where "close but not quite" pronunciations become misfires — the
    /// core Plunderspell mechanic.
    ///
    /// Resolution order:
    ///   1. Exact match on normalised text            -> the intended spell.
    ///   2. Near match (Levenshtein &lt;= lexicon max) -> that word's misfireId.
    ///   3. Nothing close                             -> SpellId.None (silent fizzle).
    /// </summary>
    public static class MisfireEngine
    {
        /// <summary>
        /// Resolve the final <see cref="SpellId"/> for a recognition result against a lexicon.
        /// </summary>
        public static SpellId Resolve(VoiceRecognitionResult result, SpellLexicon lexicon)
        {
            if (lexicon == null)
                return SpellId.None;

            string normalized = result.NormalizedText;
            if (string.IsNullOrEmpty(normalized))
                return SpellId.None;

            // 1. Exact match -> intended spell.
            var exact = lexicon.FindByWord(normalized);
            if (exact != null)
                return exact.spellId;

            // 2. Near match -> misfire. An entry that forgot to author a misfire falls back to the
            // catalogue's default rather than casting the real spell: the mechanic must not fail open.
            var near = lexicon.FindByNearMatch(normalized);
            if (near != null)
            {
                return near.misfireId != SpellId.None
                    ? near.misfireId
                    : SpellCatalogue.DefaultMisfireFor(near.spellId);
            }

            // 3. No match -> silent fail.
            return SpellId.None;
        }

        /// <summary>
        /// Convenience overload for callers/tests that only have the text.
        /// </summary>
        public static SpellId Resolve(string normalizedText, SpellLexicon lexicon)
        {
            var result = new VoiceRecognitionResult(
                normalizedText, normalizedText, 1f, 0f, CastVolume.Normal);
            return Resolve(result, lexicon);
        }

        /// <summary>
        /// Standard iterative (two-row) dynamic-programming Levenshtein edit distance.
        /// Case-sensitive: callers should pass already-normalised (uppercase) strings.
        /// </summary>
        public static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return string.IsNullOrEmpty(b) ? 0 : b.Length;
            if (string.IsNullOrEmpty(b))
                return a.Length;

            int n = a.Length;
            int m = b.Length;

            var previous = new int[m + 1];
            var current = new int[m + 1];

            for (int j = 0; j <= m; j++)
                previous[j] = j;

            for (int i = 1; i <= n; i++)
            {
                current[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    int deletion = previous[j] + 1;
                    int insertion = current[j - 1] + 1;
                    int substitution = previous[j - 1] + cost;

                    int min = deletion < insertion ? deletion : insertion;
                    if (substitution < min) min = substitution;
                    current[j] = min;
                }

                // swap rows
                var tmp = previous;
                previous = current;
                current = tmp;
            }

            return previous[m];
        }
    }
}

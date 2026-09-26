using UnityEngine;
using UnityEngine.Serialization;

namespace RogueAi.Spells
{
    /// <summary>
    /// Authored lexicon entry linking a spoken trigger word to a spell, plus the set of
    /// near-match pronunciations that should "misfire" into a (usually harmful) alternate
    /// outcome. One asset per primary spell lives under Assets/_Project/Data/Spells/.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellWord", menuName = "Plunderspell/Spell Word")]
    public class SpellWord : ScriptableObject
    {
        // NOTE: this field cannot be called "SpellWord" — C# forbids a member sharing its enclosing
        // type's name (CS0542), which broke the whole Spells assembly. FormerlySerializedAs keeps
        // any asset still authored with the old key deserialising into this one.
        [FormerlySerializedAs("SpellWord")]
        [Tooltip("The exact trigger word/phrase, normalised (UPPERCASE, no punctuation), e.g. \"IGNIS\".")]
        public string Word;

        [Tooltip("Spell that fires on an exact match.")]
        public SpellId spellId = SpellId.None;

        [Tooltip("Mana spent when this word is cast, whether it lands or misfires. A fizzle (no " +
                 "word recognised) costs nothing.")]
        [Min(0)] public int ManaCost = 15;

        [Tooltip("Near-match mispronunciations that should trigger the misfire instead, e.g. \"AGNIS\".")]
        public string[] AltPronunciations;

        [Tooltip("Spell that fires when the player says a near-match (a misfire).")]
        public SpellId misfireId = SpellId.None;

        [Header("Speech recogniser spellings")]
        [Tooltip("English spellings the speech model outputs when this word is said correctly, e.g. " +
                 "\"igneous\" for IGNIS. The model has no Latin, so these are what it can actually hear. " +
                 "Tune with Plunderspell/Voice/Speech Test.")]
        public string[] HeardAs;

        [Tooltip("English spellings the speech model outputs for the mispronounced version, e.g. " +
                 "\"egg nice\" for AGNIS. Each one misfires.")]
        public string[] MisfireHeardAs;

        [TextArea]
        [Tooltip("Human-readable description of the spell and its misfire behaviour.")]
        public string Description;

        private void OnValidate()
        {
            // Keep the authored trigger word normalised so runtime matching is exact.
            if (!string.IsNullOrEmpty(Word))
                Word = Word.Trim().ToUpperInvariant();
        }
    }
}

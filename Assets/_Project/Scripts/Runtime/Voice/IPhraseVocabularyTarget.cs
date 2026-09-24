using System.Collections.Generic;

namespace RogueAi.Voice
{
    /// <summary>
    /// A voice provider that can be told which phrases to listen for. The map runs from what the
    /// recogniser can literally hear (an English spelling, e.g. "igneous") to the word the spell
    /// lexicon matches (e.g. "IGNIS"). Lives in Voice, not Spells, because Spells depends on Voice.
    /// </summary>
    public interface IPhraseVocabularyTarget
    {
        void SetVocabulary(IReadOnlyDictionary<string, string> heardToCanonical);
    }
}

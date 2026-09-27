using System;
using System.Collections.Generic;

namespace Plunderspell.Voice
{
    /// <summary>
    /// Real speech and the keyboard at once. Holding push-to-cast opens both: speak a word, or tap
    /// a number key, and either one casts. Without this a machine with a microphone lost the
    /// keyboard fallback entirely, since the locator only ever picked one provider.
    /// </summary>
    public class CombinedVoiceInputService : IVoiceInputService, IPhraseVocabularyTarget
    {
        public VoskVoiceInputService Speech { get; }
        public MockVoiceInputService Keyboard { get; }

        public bool IsListening => Speech.IsListening || Keyboard.IsListening;
        public event Action<VoiceRecognitionResult> OnPhraseRecognized;

        public CombinedVoiceInputService(VoskVoiceInputService speech, MockVoiceInputService keyboard)
        {
            Speech = speech;
            Keyboard = keyboard;
            Speech.OnPhraseRecognized += Forward;
            Keyboard.OnPhraseRecognized += Forward;
        }

        public void StartListening()
        {
            Keyboard.StartListening();
            Speech.StartListening();
        }

        public void StopListening()
        {
            Keyboard.StopListening();
            Speech.StopListening();
        }

        public void SetVocabulary(IReadOnlyDictionary<string, string> heardToCanonical) =>
            Speech.SetVocabulary(heardToCanonical);

        private void Forward(VoiceRecognitionResult result) => OnPhraseRecognized?.Invoke(result);
    }
}

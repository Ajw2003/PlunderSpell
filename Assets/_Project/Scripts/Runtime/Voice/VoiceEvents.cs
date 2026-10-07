using EventSystems;

namespace Plunderspell.Voice
{
    // Voice payloads, published on EventManager by the voice services on the main thread (#301).

    /// <summary>A voice service recognised a phrase (speech or keyboard).</summary>
    public readonly struct PhraseRecognized : IEvent
    {
        public readonly VoiceRecognitionResult Result;
        public PhraseRecognized(VoiceRecognitionResult result) { Result = result; }
    }

    /// <summary>A voice service heard ordinary talk between casts.</summary>
    public readonly struct ChatterHeard : IEvent
    {
        public readonly ChatterReport Report;
        public ChatterHeard(ChatterReport report) { Report = report; }
    }
}

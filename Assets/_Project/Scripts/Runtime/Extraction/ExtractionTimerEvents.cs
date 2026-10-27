using EventSystems;

namespace Plunderspell.Extraction
{
    /// <summary>The seconds left in the raid changed (#302). Published on every peer, on each change.</summary>
    public readonly struct ExtractionTimerChanged : IEvent
    {
        public readonly float SecondsRemaining;
        public ExtractionTimerChanged(float secondsRemaining) { SecondsRemaining = secondsRemaining; }
    }
}

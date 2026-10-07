using EventSystems;

namespace Plunderspell.Voice
{
    /// <summary>The microphone's loudness (RMS) changed; published on each change while the service reads it (#302).</summary>
    public readonly struct MicLevelChanged : IEvent
    {
        public readonly float Level;
        public MicLevelChanged(float level) { Level = level; }
    }
}

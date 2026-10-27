using EventSystems;

namespace Plunderspell.Acoustics
{
    /// <summary>A spoken line was resolved on the speaker's machine, with what it did (#301).</summary>
    public readonly struct ChatterResolved : IEvent
    {
        public readonly ChatterOutcome Outcome;
        public ChatterResolved(ChatterOutcome outcome) { Outcome = outcome; }
    }
}

using EventSystems;

namespace Plunderspell.Alarm
{
    /// <summary>The castle's alarm level (0..100) changed (#302). Published on every peer, on each change.</summary>
    public readonly struct AlarmLevelChanged : IEvent
    {
        public readonly float Level;
        public AlarmLevelChanged(float level) { Level = level; }
    }
}

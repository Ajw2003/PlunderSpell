using EventSystems;

namespace Interfaces
{
    /// <summary>A hit cost something health (#301). Published by <see cref="Damage"/> on every peer that learns of it.</summary>
    public readonly struct DamageDealt : IEvent
    {
        public readonly DamageReport Report;
        public DamageDealt(DamageReport report) { Report = report; }
    }
}

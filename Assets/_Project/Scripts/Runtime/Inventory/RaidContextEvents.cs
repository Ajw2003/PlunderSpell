using EventSystems;

namespace Plunderspell.Inventory
{
    /// <summary>A raid published its context on this peer (#300).</summary>
    public readonly struct RaidContextPublished : IEvent
    {
        public readonly RaidContext Context;
        public RaidContextPublished(RaidContext context) { Context = context; }
    }
}

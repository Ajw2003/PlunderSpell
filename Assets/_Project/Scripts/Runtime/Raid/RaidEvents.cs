using EventSystems;
using UnityEngine;

namespace Plunderspell.Raid
{
    // The raid's payloads, published on EventManager (#300).

    /// <summary>The extraction portal opened at <see cref="Point"/>.</summary>
    public readonly struct PortalOpened : IEvent
    {
        public readonly Vector3 Point;
        public PortalOpened(Vector3 point) { Point = point; }
    }

    /// <summary>The raid moved to another phase.</summary>
    public readonly struct RaidPhaseChanged : IEvent
    {
        public readonly RaidPhase Phase;
        public RaidPhaseChanged(RaidPhase phase) { Phase = phase; }
    }

    /// <summary>The raid is over: what the party carried out and how many players were saved.</summary>
    public readonly struct RaidResolved : IEvent
    {
        public readonly float Worth;
        public readonly int Saved;
        public RaidResolved(float worth, int saved) { Worth = worth; Saved = saved; }
    }
}

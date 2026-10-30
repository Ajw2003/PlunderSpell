using EventSystems;
using UnityEngine;

namespace Plunderspell.Extraction
{
    /// <summary>What the portal is doing: the way in, shut, or the hidden way out.</summary>
    public enum PortalState { Arrival, Closed, Escape }

    /// <summary>The portal changed state; its floor point is where it stands now. Published on every peer.</summary>
    public readonly struct PortalStateChanged : IEvent
    {
        public readonly PortalState State;
        public readonly Vector3 FloorPoint;
        public PortalStateChanged(PortalState state, Vector3 floorPoint) { State = state; FloorPoint = floorPoint; }
    }

    /// <summary>
    /// When the portal is where. Pure, so every peer derives the same state from the raid clock
    /// and nothing needs replicating.
    /// </summary>
    public static class PortalSchedule
    {
        // Matches GuardArrivalGrace.Seconds (Extraction cannot reference Guards); a test asserts they are equal.
        /// <summary>Seconds of the raid the arrival portal stays open.</summary>
        public const float ArrivalSeconds = 20f;

        /// <summary>The state after <paramref name="elapsed"/> seconds, with <paramref name="remaining"/> left. Escape wins over Arrival.</summary>
        public static PortalState At(float elapsed, float remaining, float arrivalSeconds, float huntWindowSeconds)
        {
            if (remaining <= huntWindowSeconds)
                return PortalState.Escape;
            return elapsed < arrivalSeconds ? PortalState.Arrival : PortalState.Closed;
        }
    }
}

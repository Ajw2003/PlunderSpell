using EventSystems;

namespace Plunderspell.UI
{
    // Held-key HUD panels, published on EventManager by HudHoldKeys (#324, #325).

    /// <summary>The pocket watch was raised (<see cref="Up"/> true, the player holds <c>T</c>) or lowered.</summary>
    public readonly struct WatchRaised : IEvent
    {
        public readonly bool Up;
        public WatchRaised(bool up) { Up = up; }
    }
}

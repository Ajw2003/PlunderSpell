using EventSystems;

namespace Plunderspell.Loot
{
    // Loot payloads, published on EventManager (#301).

    /// <summary>A piece was ruined, and the worth it just lost.</summary>
    public readonly struct LootRuined : IEvent
    {
        public readonly LootValue Piece;
        public readonly float WorthLost;
        public LootRuined(LootValue piece, float worthLost) { Piece = piece; WorthLost = worthLost; }
    }

    /// <summary>The door handle the player is looking at changed; null when none is in focus.</summary>
    public readonly struct DoorFocusChanged : IEvent
    {
        public readonly CastleDoorHandle Focus;
        public DoorFocusChanged(CastleDoorHandle focus) { Focus = focus; }
    }

    /// <summary>The loot the player is looking at changed; null when nothing is in focus.</summary>
    public readonly struct LootFocusChanged : IEvent
    {
        public readonly LootPickup Focus;
        public LootFocusChanged(LootPickup focus) { Focus = focus; }
    }
}

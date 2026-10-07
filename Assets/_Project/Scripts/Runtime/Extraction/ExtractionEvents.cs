using System.Collections.Generic;
using EventSystems;
using Plunderspell.Loot;

namespace Plunderspell.Extraction
{
    // The extraction zone's payloads, published on EventManager (#300).

    /// <summary>The extraction resolved on this peer: worth extracted and players saved.</summary>
    public readonly struct ExtractionResolved : IEvent
    {
        public readonly float Worth;
        public readonly int Saved;
        public ExtractionResolved(float worth, int saved) { Worth = worth; Saved = saved; }
    }

    /// <summary>
    /// Server only: the unbroken pieces that went through the portal, as their authored loot definitions (#310).
    /// The Lair spawns them again on its flagstones.
    /// </summary>
    public readonly struct HaulExtracted : IEvent
    {
        public readonly IReadOnlyList<LootItem> Pieces;
        public HaulExtracted(IReadOnlyList<LootItem> pieces) { Pieces = pieces; }
    }

    /// <summary>Loot entered or left the zone: the haul's worth and piece count now. The HUD shows a running total from this.</summary>
    public readonly struct HaulInZoneChanged : IEvent
    {
        public readonly float Worth;
        public readonly int Pieces;
        public HaulInZoneChanged(float worth, int pieces) { Worth = worth; Pieces = pieces; }
    }
}

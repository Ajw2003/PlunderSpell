using EventSystems;

namespace Plunderspell.Raid
{
    /// <summary>The ledger's demand page was torn out, changed hands or went back into the book (#448). Holder is -1 in the book, -2 while it lies on the floor.</summary>
    public readonly struct DemandPageChanged : IEvent
    {
        public readonly int HolderSeat;
        public DemandPageChanged(int holderSeat) { HolderSeat = holderSeat; }
        public bool IsTorn => DemandPageRules.IsTorn(HolderSeat);
        public bool IsLying => HolderSeat == DemandPageRules.Lying;
    }
}

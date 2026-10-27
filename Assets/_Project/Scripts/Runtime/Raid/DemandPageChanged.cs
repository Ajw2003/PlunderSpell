using EventSystems;

namespace Plunderspell.Raid
{
    /// <summary>The ledger's demand page was torn out, changed hands or went back into the book (#448). Holder is -1 in the book.</summary>
    public readonly struct DemandPageChanged : IEvent
    {
        public readonly int HolderSeat;
        public DemandPageChanged(int holderSeat) { HolderSeat = holderSeat; }
        public bool IsTorn => DemandPageRules.IsTorn(HolderSeat);
    }
}

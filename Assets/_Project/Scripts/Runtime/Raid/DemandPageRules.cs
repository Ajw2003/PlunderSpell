namespace Plunderspell.Raid
{
    /// <summary>
    /// The rules for the Lair ledger's torn-out left page (#448), apart from the network: who holds it is a seat
    /// (0 to 3, owner id n is seat n - 1), or <see cref="InBook"/> when it is still in the book. Steps #449 and #450
    /// add a "dropped at a position" state beside the holder; the holder stays the one number that says where the page is.
    /// </summary>
    public static class DemandPageRules
    {
        public const int InBook = -1;

        /// <summary>The holder after this seat tries to tear the page out: only a page still in the book can be torn.</summary>
        public static int Tear(int holder, int seat) => holder == InBook ? seat : holder;

        /// <summary>Whether the page is out of the book.</summary>
        public static bool IsTorn(int holder) => holder != InBook;
    }
}

namespace Plunderspell.Raid
{
    /// <summary>
    /// The rules for the Lair ledger's torn-out left page (#448), apart from the network: who holds it is a seat
    /// (0 to 3, owner id n is seat n - 1), or <see cref="InBook"/> when it is still in the book. A dropped page (#450) is
    /// <see cref="Lying"/>, its place kept apart by the keeper; the holder stays the one number that says where the page is.
    /// </summary>
    public static class DemandPageRules
    {
        public const int InBook = -1;

        /// <summary>The page was dropped (#450) and lies on the floor; it still counts as torn, so the book keeps its stub.</summary>
        public const int Lying = -2;

        /// <summary>The holder after this seat tries to tear the page out: only a page still in the book can be torn.</summary>
        public static int Tear(int holder, int seat) => holder == InBook ? seat : holder;

        /// <summary>The holder after this seat drops the page: only the seat holding it can.</summary>
        public static int Drop(int holder, int seat) => holder == seat && seat >= 0 ? Lying : holder;

        /// <summary>The holder after this seat picks the page up: only a page lying on the floor can be.</summary>
        public static int PickUp(int holder, int seat) => holder == Lying ? seat : holder;

        /// <summary>Whether Q drops the page rather than loot: the player holds the page and has empty hands.</summary>
        public static bool QDropsPage(bool holdsPage, bool carryingLoot) => holdsPage && !carryingLoot;

        /// <summary>Whether the page is out of the book.</summary>
        public static bool IsTorn(int holder) => holder != InBook;
    }
}

namespace Plunderspell.Castle
{
    /// <summary>An archway as one room sees it: which link, and the floor cell it opens onto.</summary>
    public readonly struct NavLinkEnd
    {
        public readonly int Link;
        public readonly int Cell;

        public NavLinkEnd(int link, int cell)
        {
            Link = link;
            Cell = cell;
        }
    }
}

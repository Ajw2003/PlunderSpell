namespace Plunderspell.Castle
{
    /// <summary>
    /// A way across one room from an archway to another, with its walking cost. Computed once when
    /// the graph is built, so the portal search never touches the room's cells.
    /// </summary>
    public readonly struct NavEdge
    {
        public readonly int ToLink;
        public readonly int ThroughModule;
        public readonly float Cost;

        public NavEdge(int toLink, int throughModule, float cost)
        {
            ToLink = toLink;
            ThroughModule = throughModule;
            Cost = cost;
        }
    }
}

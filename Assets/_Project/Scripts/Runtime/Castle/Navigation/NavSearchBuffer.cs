namespace Plunderspell.Castle
{
    /// <summary>
    /// Scratch memory for one search inside one room: cost so far and where each cell was reached
    /// from. Entries are stamped with the search that wrote them, so starting a new search is a
    /// counter bump rather than clearing arrays, and a stale entry reads as "not reached".
    /// </summary>
    public sealed class NavSearchBuffer
    {
        /// <summary>Parent value of the cell a search started from.</summary>
        public const short NoParent = -1;

        private readonly float[] _cost = new float[CastleNavGrid.CellsPerModule];
        private readonly short[] _parent = new short[CastleNavGrid.CellsPerModule];
        private readonly int[] _reachedBy = new int[CastleNavGrid.CellsPerModule];
        private readonly int[] _closedBy = new int[CastleNavGrid.CellsPerModule];
        private int _search;

        /// <summary>Starts a new search; everything reached before now reads as not reached.</summary>
        public void Begin()
        {
            _search++;
        }

        /// <summary>Cost to reach a room-local cell in this search, or infinity if it was not reached.</summary>
        public float CostTo(int localCell)
        {
            return _reachedBy[localCell] == _search ? _cost[localCell] : float.PositiveInfinity;
        }

        /// <summary>The room-local cell this one was reached from, or <see cref="NoParent"/>.</summary>
        public int ParentOf(int localCell)
        {
            return _parent[localCell];
        }

        /// <summary>Records the best known way to a cell.</summary>
        public void Reach(int localCell, float cost, int parent)
        {
            _reachedBy[localCell] = _search;
            _cost[localCell] = cost;
            _parent[localCell] = (short)parent;
        }

        /// <summary>True once a cell has been settled in this search.</summary>
        public bool IsClosed(int localCell)
        {
            return _closedBy[localCell] == _search;
        }

        /// <summary>Settles a cell. Returns false if it already was, so stale heap entries are skipped.</summary>
        public bool TryClose(int localCell)
        {
            if (_closedBy[localCell] == _search)
                return false;
            _closedBy[localCell] = _search;
            return true;
        }
    }
}

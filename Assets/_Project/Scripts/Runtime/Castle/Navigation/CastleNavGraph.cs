using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// The castle's walkable map, built from the seed's layout so every peer gets the same one and
    /// nothing is sent over the network (#221, docs/plans/bespoke-navigation.md Design 2).
    ///
    /// It has two layers. The fine layer is a grid of 0.5 m cells inside every room (the baked tile
    /// of that room, turned to the way it was placed). The coarse layer is a short list of open
    /// archways, with the cost of walking across each room between them. A path query picks the
    /// archways on the coarse layer, which is tiny, then walks each room on the fine layer. Whether
    /// one place can reach another is a single comparison, because cells connected through open
    /// archways share an area id.
    ///
    /// Plain C#, no scene or physics access. Not thread safe: queries reuse preallocated buffers, so
    /// once they have grown during the first calls nothing is allocated per query.
    /// </summary>
    public sealed class CastleNavGraph
    {
        private CastleNavGrid _grid;
        private CastleNavPortalGraph _portals;
        private CastleNavPathFinder _pathFinder;
        private readonly List<Vector3> _scratchPath = new List<Vector3>();

        private CastleNavGraph() { }

        /// <summary>Modules in the layout this graph was built from (walkable or not).</summary>
        public int ModuleCount => _grid != null ? _grid.ModuleCount : 0;

        /// <summary>Open archways: the coarse layer's nodes.</summary>
        public int LinkCount => _portals != null ? _portals.LinkCount : 0;

        /// <summary>Walkable cells over the whole castle.</summary>
        public int WalkableCellCount => _grid != null ? _grid.WalkableCellCount : 0;

        /// <summary>
        /// Builds the graph for a generated layout. A module with no baked tile in the registry
        /// contributes no cells, and archways stay joined only where the generator left them open.
        /// Without a registry or a layout the graph is empty and every query answers "nowhere".
        /// </summary>
        public static CastleNavGraph Build(ProceduralCastleData data, CastleRoomRegistry registry)
        {
            var graph = new CastleNavGraph();
            if (data?.PlacedModules == null || data.PlacedModules.Count == 0 || registry == null)
                return graph;

            var grid = new CastleNavGrid(data.PlacedModules);
            if (!grid.IsUsable)
                return graph;
            CastleNavStitcher.LoadTiles(grid, data, registry);
            CastleNavAreas areas = CastleNavAreas.Label(grid);
            List<NavLink> links = CastleNavStitcher.FindLinks(grid, data, areas);
            areas.Flatten(grid);

            var heap = new NavMinHeap();
            var gridSearch = new CastleNavGridSearch(grid, heap);
            var portals = new CastleNavPortalGraph(links, grid.ModuleCount);
            portals.ComputeRoomCrossings(gridSearch, grid.ModuleCount);

            graph._grid = grid;
            graph._portals = portals;
            graph._pathFinder = new CastleNavPathFinder(grid, portals, gridSearch,
                new CastleNavPortalSearch(portals, heap));
            return graph;
        }

        /// <summary>World position of a cell's centre, at standing height.</summary>
        public Vector3 CellPosition(int cell) => _grid.CellPosition(cell);

        /// <summary>
        /// The walkable cell nearest a world point (3D distance, so a gallery and the floor under it
        /// are told apart), or <see cref="CastleNavGrid.NoCell"/> if none is within the radius.
        /// </summary>
        public int NearestWalkableCell(Vector3 position, float maxDistance = 3f)
        {
            return _grid == null ? CastleNavGrid.NoCell : _grid.NearestWalkableCell(position, maxDistance);
        }

        /// <summary>
        /// True when a guard could walk from one cell to the other through archways that are not
        /// closed. With no door closed this is a single area-id comparison; with one closed it also
        /// searches, because area ids are computed once and know nothing of doors.
        /// </summary>
        public bool IsReachable(int cellA, int cellB)
        {
            if (!IsReachableWithDoorsOpen(cellA, cellB))
                return false;
            return _portals.ClosedLinkCount == 0 || _pathFinder.Find(cellA, cellB, _scratchPath);
        }

        /// <summary>Reachability as the castle was built, ignoring every door's state.</summary>
        public bool IsReachableWithDoorsOpen(int cellA, int cellB)
        {
            return cellA >= 0 && cellB >= 0 && _grid.AreaId(cellA) >= 0 && _grid.AreaId(cellA) == _grid.AreaId(cellB);
        }

        /// <summary>Extra cost that marks a door as shut (<see cref="SetDoorCost"/>).</summary>
        public const float ClosedDoor = float.PositiveInfinity;

        /// <summary>Changes whenever a door's cost changes; routes planned before are stale.</summary>
        public int DoorVersion => _portals != null ? _portals.DoorVersion : 0;

        /// <summary>The open archway nearest a point (floor plan distance only), or -1. How a door finds its link.</summary>
        public int FindLinkNear(Vector3 position, float maxDistance)
        {
            return _portals == null ? -1 : _portals.FindLinkNear(position, maxDistance);
        }

        /// <summary>
        /// Prices crossing a link on top of walking it (#222). Zero is ordinary, a positive cost makes
        /// paths detour round it, <see cref="ClosedDoor"/> removes it. Nothing is rebuilt.
        /// </summary>
        public void SetDoorCost(int link, float extraCost)
        {
            if (_portals != null && link >= 0 && link < _portals.LinkCount)
                _portals.SetExtraCost(link, extraCost);
        }

        /// <summary>Opens every door and removes every door cost.</summary>
        public void ClearDoorCosts() => _portals?.ClearExtraCosts();

        /// <summary>True when the walkable cells nearest the two points are connected.</summary>
        public bool IsReachable(Vector3 a, Vector3 b)
        {
            return IsReachable(NearestWalkableCell(a), NearestWalkableCell(b));
        }

        /// <summary>
        /// Fills <paramref name="path"/> with cell centres from the cell nearest <paramref name="from"/>
        /// to the one nearest <paramref name="to"/>. Returns false, path empty, when either point has
        /// no walkable cell or the two are not connected. The path follows 4-connected cells, so
        /// smoothing is up to whatever moves along it.
        /// </summary>
        public bool FindPath(Vector3 from, Vector3 to, List<Vector3> path)
        {
            path.Clear();
            int startNode = NearestWalkableCell(from);
            int goalNode = NearestWalkableCell(to);
            return IsReachableWithDoorsOpen(startNode, goalNode) && _pathFinder.Find(startNode, goalNode, path);
        }

        /// <summary>Order-sensitive hash of everything the build produced, to compare two builds.</summary>
        public int Checksum()
        {
            if (_grid == null)
                return 0;
            unchecked
            {
                return _grid.Checksum() * 31 + _portals.Checksum();
            }
        }
    }
}

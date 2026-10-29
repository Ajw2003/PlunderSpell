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
        // Kept so a break can rebuild: the layout the archway links are found from again, and the links
        // a broken wall opened (the archway links are not kept, they are found again from the cells).
        private ProceduralCastleData _data;
        private readonly List<NavLink> _holeLinks = new List<NavLink>();
        private static readonly Collider[] s_colliders = new Collider[32];
        private static readonly RaycastHit[] s_hits = new RaycastHit[32];
        private static readonly int[] s_stepColumn = { 1, -1, 0, 0 };
        private static readonly int[] s_stepRow = { 0, 0, 1, -1 };
        private const int MaxGrowPasses = 6;

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
        /// <paramref name="obstacles"/> are things placed on top of the rooms (the bailey dressing); the
        /// cells they stand on are taken out (<see cref="CastleNavObstacles"/>). They must be in place in
        /// the physics scene already. Without a registry or a layout the graph is empty and every query
        /// answers "nowhere".
        /// </summary>
        public static CastleNavGraph Build(ProceduralCastleData data, CastleRoomRegistry registry,
            IReadOnlyList<GameObject> obstacles = null)
        {
            var graph = new CastleNavGraph();
            if (data?.PlacedModules == null || data.PlacedModules.Count == 0 || registry == null)
                return graph;

            var grid = new CastleNavGrid(data.PlacedModules);
            if (!grid.IsUsable)
                return graph;
            CastleNavStitcher.LoadTiles(grid, data, registry);
            CastleNavSealedArchways.Clear(grid, data);
            int stamped = CastleNavObstacles.Stamp(grid, obstacles); // before areas, so a cart that seals a gap splits them
            if (obstacles != null && obstacles.Count > 0)
                Debug.Log($"[CastleNav] {obstacles.Count} dressing pieces took {stamped} cells out of the walk map.");
            graph._grid = grid;
            graph._data = data;
            graph.Connect();
            return graph;
        }

        // Areas, archway links and the room crossings from the cells as they stand, plus the links a break opened.
        private void Connect()
        {
            CastleNavAreas areas = CastleNavAreas.Label(_grid);
            List<NavLink> links = CastleNavStitcher.FindLinks(_grid, _data, areas);
            foreach (NavLink hole in _holeLinks)
            {
                if (!_grid.IsWalkable(hole.CellA) || !_grid.IsWalkable(hole.CellB))
                    continue;
                areas.Join(_grid, hole.CellA, hole.CellB);
                links.Add(hole);
            }
            areas.Flatten(_grid);

            var heap = new NavMinHeap();
            var gridSearch = new CastleNavGridSearch(_grid, heap);
            _portals = new CastleNavPortalGraph(links, _grid.ModuleCount);
            _portals.ComputeRoomCrossings(gridSearch, _grid.ModuleCount);
            _pathFinder = new CastleNavPathFinder(_grid, _portals, gridSearch, new CastleNavPortalSearch(_portals, heap));
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

        /// <summary>The floor cell under a point, chosen by its own column first (<see cref="CastleNavFloorLookup"/>).</summary>
        public int FloorCellUnder(Vector3 position, float maxDistance)
        {
            return _grid == null ? CastleNavGrid.NoCell : CastleNavFloorLookup.Find(_grid, position, maxDistance);
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

        // --- A break changed the castle (#478) -----------------------------------------------------

        /// <summary>True when a guard can stand on the cell.</summary>
        public bool IsWalkable(int cell) => _grid != null && cell >= 0 && _grid.IsWalkable(cell);

        /// <summary>Takes a cell out of the walk map. Takes effect for paths and reachability at the next <see cref="Rebuild"/>.</summary>
        public void ClearCell(int cell) => _grid?.ClearWalkable(cell);

        /// <summary>Adds a link between two cells of different rooms, as a hole in a wall makes. Takes effect at the next <see cref="Rebuild"/>.</summary>
        public void AddLink(int cellA, int cellB)
        {
            foreach (NavLink hole in _holeLinks)
            {
                if (hole.CellA == cellA && hole.CellB == cellB)
                    return;
            }
            _holeLinks.Add(new NavLink(CastleNavGrid.ModuleOf(cellA), CastleNavGrid.ModuleOf(cellB), cellA, cellB, _grid.CellPosition(cellA)));
        }

        /// <summary>The two cells a link joins, or false for no such link.</summary>
        public bool TryGetLinkCells(int link, out int cellA, out int cellB)
        {
            cellA = cellB = CastleNavGrid.NoCell;
            if (_portals == null || link < 0 || link >= _portals.LinkCount)
                return false;
            cellA = _portals.Link(link).CellA;
            cellB = _portals.Link(link).CellB;
            return true;
        }

        /// <summary>What crossing a link costs beyond the walk (<see cref="SetDoorCost"/>), or zero for no such link.</summary>
        public float DoorCost(int link) => _portals != null && link >= 0 && link < _portals.LinkCount ? _portals.ExtraCost(link) : 0f;

        /// <summary>
        /// Relabels the areas and rebuilds the links and room crossings from the cells as they now stand, the way
        /// <see cref="Build"/> did. Link numbers change, so each door's cost is moved to the link now nearest its archway,
        /// and <see cref="DoorVersion"/> goes up so routes planned before are dropped.
        /// </summary>
        public void Rebuild()
        {
            if (_grid == null)
                return;
            CastleNavPortalGraph old = _portals;
            _grid.ResetAreas();
            Connect();
            _portals.StartVersionAt(old.DoorVersion + 1);
            for (int link = 0; link < old.LinkCount; link++)
            {
                if (old.ExtraCost(link) == 0f)
                    continue;
                NavLink was = old.Link(link);
                int now = _portals.FindLinkBetween(was.ModuleA, was.ModuleB, was.Position, CastleLockdownNavigation.DoorToArchwayDistance);
                if (now >= 0)
                    _portals.SetExtraCost(now, old.ExtraCost(link));
                else
                    Debug.LogWarning($"[CastleNav] The archway of a locked or closed door between modules {was.ModuleA} and {was.ModuleB} no longer exists; its door state is dropped.");
            }
        }

        /// <summary>
        /// Brings the map up to date with a charged beam that broke castle pieces along <paramref name="from"/> to
        /// <paramref name="to"/>, within <paramref name="radius"/> of that line (host only; colliders must already have
        /// changed). Cells whose floor is gone or that a wall now overlaps are cleared; cells a broken wall freed are
        /// added; where a wall between two rooms has a hole a guard fits through, a link joins the two sides; then
        /// <see cref="Rebuild"/>. Uses the baker's own standing test (<see cref="CastleNavProbe"/>) against the castle's colliders only.
        /// </summary>
        public void RefreshAfterBreak(Vector3 from, Vector3 to, float radius)
        {
            System.Collections.IEnumerator steps = RefreshAfterBreakSpread(from, to, radius, float.PositiveInfinity);
            while (steps.MoveNext()) { }
        }

        /// <summary>
        /// <see cref="RefreshAfterBreak"/> spread over frames: yields whenever a frame has spent
        /// <paramref name="budgetMs"/> on it (all at once on the host cost 20-50 ms; aj's frame pacing, 2026-10-29).
        /// The rebuild at the end is one step.
        /// </summary>
        public System.Collections.IEnumerator RefreshAfterBreakSpread(Vector3 from, Vector3 to, float radius, float budgetMs)
        {
            if (_grid == null || _data == null)
                yield break;
            var frame = System.Diagnostics.Stopwatch.StartNew();
            double totalMs = 0;
            int frames = 1;
            PhysicsScene physics = Physics.defaultPhysicsScene;
            int firstColumn = _grid.CastleColumn(Mathf.Min(from.x, to.x) - radius), lastColumn = _grid.CastleColumn(Mathf.Max(from.x, to.x) + radius);
            int firstRow = _grid.CastleRow(Mathf.Min(from.z, to.z) - radius), lastRow = _grid.CastleRow(Mathf.Max(from.z, to.z) + radius);

            int cleared = 0, added = 0;
            for (int row = firstRow; row <= lastRow; row++)
            {
                for (int column = firstColumn; column <= lastColumn; column++)
                    cleared += ClearBrokenColumn(physics, column, row, from, to, radius);
                if (frame.Elapsed.TotalMilliseconds >= budgetMs) { totalMs += frame.Elapsed.TotalMilliseconds; frames++; yield return null; frame.Restart(); }
            }
            for (int pass = 0; pass < MaxGrowPasses; pass++)
            {
                int grown = 0;
                for (int row = firstRow; row <= lastRow; row++)
                {
                    for (int column = firstColumn; column <= lastColumn; column++)
                        grown += GrowColumn(physics, column, row, from, to, radius);
                    if (frame.Elapsed.TotalMilliseconds >= budgetMs) { totalMs += frame.Elapsed.TotalMilliseconds; frames++; yield return null; frame.Restart(); }
                }
                added += grown;
                if (grown == 0)
                    break;
            }
            int holes = LinkHoles(from, to, radius);
            int linksBefore = LinkCount;
            double scanMs = totalMs + frame.Elapsed.TotalMilliseconds;
            if (frame.Elapsed.TotalMilliseconds >= budgetMs) { frames++; yield return null; }
            frame.Restart();
            if (cleared + added + holes > 0)
                Rebuild();
            Debug.Log($"[CastleNav] Break refresh: {cleared} cells cleared, {added} added, {holes} wall hole links, links {linksBefore} -> {LinkCount}; " +
                      $"{(lastColumn - firstColumn + 1) * (lastRow - firstRow + 1)} columns scanned in {scanMs:F1} ms over {frames} frames, rebuild {frame.Elapsed.TotalMilliseconds:F1} ms in one.");
        }

        // A cell whose floor is gone (rooms only: a curtain piece stands on the scene's ground) or that the castle now overlaps.
        private int ClearBrokenColumn(PhysicsScene physics, int column, int row, Vector3 from, Vector3 to, float radius)
        {
            int cleared = 0;
            if (!ColumnNearSegment(column, row, from, to, radius))
                return 0;
            for (int level = CastleLevels.Lowest; level < CastleLevels.Lowest + CastleLevels.Count; level++)
            {
                for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                {
                    int cell = _grid.CellAt(column, row, layer, level);
                    if (cell < 0 || !_grid.IsWalkable(cell))
                        continue;
                    Vector3 floor = _grid.CellPosition(cell);
                    if (!NearSegment(floor, from, to, radius))
                        continue;
                    bool floorGone = IsRoom(CastleNavGrid.ModuleOf(cell)) && !CastleNavProbe.HasFloor(physics, floor);
                    if (floorGone || CastleNavProbe.IsBlocked(physics, floor, s_colliders, true))
                    {
                        _grid.ClearWalkable(cell);
                        cleared++;
                    }
                }
            }
            return cleared;
        }

        // A cell beside a walkable one, no more than a step off its floor, that now passes the standing test.
        private int GrowColumn(PhysicsScene physics, int column, int row, Vector3 from, Vector3 to, float radius)
        {
            int added = 0;
            if (!ColumnNearSegment(column, row, from, to, radius))
                return 0;
            float x = CastleNavGrid.CellCentre(column), z = CastleNavGrid.CellCentre(row);
            for (int direction = 0; direction < 4; direction++)
            {
                for (int level = CastleLevels.Lowest; level < CastleLevels.Lowest + CastleLevels.Count; level++)
                {
                    for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                    {
                        int beside = _grid.CellAt(column + s_stepColumn[direction], row + s_stepRow[direction], layer, level);
                        if (beside < 0 || !_grid.IsWalkable(beside))
                            continue;
                        float floorY = _grid.HeightCm(beside) / CastleNavTile.HeightScale;
                        int module = _grid.ModuleUnder(column, row, floorY);
                        if (module < 0)
                            continue;
                        if (IsRoom(module))
                        {
                            if (!physics.Raycast(new Vector3(x, floorY + CastleNavTile.StepHeight + 0.05f, z), Vector3.down, out RaycastHit hit,
                                    CastleNavTile.StepHeight * 2f + 0.1f, ~0, QueryTriggerInteraction.Ignore) || hit.normal.y < CastleNavProbe.MinNormalY)
                                continue;
                            floorY = hit.point.y;
                        }
                        int cell = _grid.FreeCellAt(module, column, row, floorY);
                        var floor = new Vector3(x, floorY, z);
                        if (cell < 0 || !NearSegment(floor, from, to, radius) || CastleNavProbe.IsBlocked(physics, floor, s_colliders, true))
                            continue;
                        _grid.SetWalkable(cell, (short)Mathf.RoundToInt(floorY * CastleNavTile.HeightScale));
                        added++;
                    }
                }
            }
            return added;
        }

        // Across each wall between two rooms that is not an open archway, the pair of cells nearest the beam that a guard can slide between.
        private int LinkHoles(Vector3 from, Vector3 to, float radius)
        {
            int added = 0;
            for (int module = 0; module < _grid.ModuleCount; module++)
            {
                if (!_grid.HasTile(module))
                    continue;
                ProceduralCastleData.PlacedModule placed = _data.PlacedModules[module];
                for (int level = placed.Level; level <= placed.TopLevel; level++)
                {
                    for (int side = 0; side < 4; side++)
                    {
                        Vector2Int at = placed.GridPosition + CastleNavGrid.SideOffset(side);
                        int other = _grid.ModuleAtGridCell(at.x, at.y, level);
                        if (other <= module || !_grid.HasTile(other) ||
                            CastleNavArchwayRule.IsOpen(_data, placed, _data.PlacedModules[other], level))
                            continue;
                        if (TryFindHole(module, side, other, from, to, radius, out int cell, out int across))
                        {
                            AddLink(cell, across);
                            added++;
                        }
                    }
                }
            }
            return added;
        }

        private bool TryFindHole(int module, int side, int other, Vector3 from, Vector3 to, float radius, out int bestCell, out int bestAcross)
        {
            bestCell = bestAcross = CastleNavGrid.NoCell;
            float bestSqr = float.MaxValue;
            for (int along = 0; along < CastleNavGrid.TileSize; along++)
            {
                for (int la = 0; la < CastleNavTile.Layers; la++)
                {
                    for (int lb = 0; lb < CastleNavTile.Layers; lb++)
                    {
                        int cell = CastleNavGrid.EdgeCell(module, side, along) + la * CastleNavGrid.ColumnsPerModule;
                        int across = CastleNavGrid.EdgeCell(other, (side + 2) % 4, along) + lb * CastleNavGrid.ColumnsPerModule;
                        if (!_grid.IsWalkable(cell) || !_grid.IsWalkable(across) || !_grid.IsStep(cell, across))
                            continue;
                        Vector3 a = _grid.CellPosition(cell), b = _grid.CellPosition(across);
                        float sqr = SegmentDistanceSqr(a, from, to);
                        if (sqr > radius * radius || sqr >= bestSqr || CastleNavProbe.SweepBlocked(a, b, s_hits))
                            continue;
                        bestSqr = sqr;
                        bestCell = cell;
                        bestAcross = across;
                    }
                }
            }
            return bestCell >= 0;
        }

        private bool IsRoom(int module) => ProceduralCastleGenerator.IsEnclosedRoom(_data.PlacedModules[module].Zone);

        // Within range if the cell's floor, middle or head is: a wall breaks at body height, a floor at its own.
        // Seen from above: a column farther than radius from the line can hold no cell near it, so its probes and casts
        // are skipped (the scan box around a break held about 800 columns, 20 ms; aj's frame pacing, 2026-10-29).
        private static bool ColumnNearSegment(int column, int row, Vector3 from, Vector3 to, float radius)
        {
            var point = new Vector3(CastleNavGrid.CellCentre(column), 0f, CastleNavGrid.CellCentre(row));
            return SegmentDistanceSqr(point, new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z)) <= radius * radius;
        }

        private static bool NearSegment(Vector3 floor, Vector3 from, Vector3 to, float radius)
        {
            float limit = radius * radius;
            return SegmentDistanceSqr(floor, from, to) <= limit ||
                   SegmentDistanceSqr(floor + Vector3.up * (CastleNavProbe.GuardHeight * 0.5f), from, to) <= limit ||
                   SegmentDistanceSqr(floor + Vector3.up * CastleNavProbe.GuardHeight, from, to) <= limit;
        }

        private static float SegmentDistanceSqr(Vector3 point, Vector3 from, Vector3 to)
        {
            Vector3 line = to - from;
            float t = line.sqrMagnitude > 0f ? Mathf.Clamp01(Vector3.Dot(point - from, line) / line.sqrMagnitude) : 0f;
            return (from + line * t - point).sqrMagnitude;
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

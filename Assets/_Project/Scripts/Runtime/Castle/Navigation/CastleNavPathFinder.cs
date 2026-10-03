using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Turns a start cell and a goal cell into a list of cell centres: the portal search picks the
    /// archways, then the grid search walks each room between them. A path inside one room skips the
    /// portal search entirely.
    /// </summary>
    public sealed class CastleNavPathFinder
    {
        private readonly CastleNavGrid _grid;
        private readonly CastleNavPortalGraph _portals;
        private readonly CastleNavGridSearch _gridSearch;
        private readonly CastleNavPortalSearch _portalSearch;

        public CastleNavPathFinder(CastleNavGrid grid, CastleNavPortalGraph portals,
            CastleNavGridSearch gridSearch, CastleNavPortalSearch portalSearch)
        {
            _grid = grid;
            _portals = portals;
            _gridSearch = gridSearch;
            _portalSearch = portalSearch;
        }

        /// <summary>Fills <paramref name="path"/> from <paramref name="startNode"/> to <paramref name="goalNode"/> (cell ids).</summary>
        public bool Find(int startNode, int goalNode, List<Vector3> path)
        {
            path.Clear();
            int startModule = CastleNavGrid.ModuleOf(startNode), goalModule = CastleNavGrid.ModuleOf(goalNode);
            int startLocal = CastleNavGrid.LocalCell(startNode), goalLocal = CastleNavGrid.LocalCell(goalNode);

            // Usually the shortest way between two cells of one room stays in it; if not, go through archways.
            if (startModule == goalModule && WalkRoom(startModule, startLocal, goalLocal, path))
                return true;

            _gridSearch.Run(_gridSearch.StartFlood, startModule, startLocal, -1);
            _gridSearch.Run(_gridSearch.GoalFlood, goalModule, goalLocal, -1);
            if (!_portalSearch.Run(startModule, goalModule, _grid.CellPosition(goalNode),
                    _gridSearch.StartFlood, _gridSearch.GoalFlood))
                return false;
            if (WalkRoute(startModule, startLocal, goalModule, goalLocal, path))
                return true;
            path.Clear();
            return false;
        }

        private bool WalkRoute(int startModule, int startLocal, int goalModule, int goalLocal, List<Vector3> path)
        {
            int module = startModule, local = startLocal, previousLink = -1;
            for (int step = 0; step < _portalSearch.RouteLength; step++)
            {
                int linkId = _portalSearch.RouteLink(step);
                int room = _portalSearch.RouteModule(step);
                if (room != module)
                {
                    local = StepAcross(previousLink, room, path);
                    module = room;
                }
                int archwayLocal = CastleNavGrid.LocalCell(_portals.Link(linkId).CellIn(module));
                if (!WalkRoom(module, local, archwayLocal, path))
                    return false;
                local = archwayLocal;
                previousLink = linkId;
            }
            if (goalModule != module)
            {
                local = StepAcross(previousLink, goalModule, path);
                module = goalModule;
            }
            return WalkRoom(module, local, goalLocal, path);
        }

        // Steps over an archway just reached into the room on its far side; returns the arrival cell.
        private int StepAcross(int linkId, int intoModule, List<Vector3> path)
        {
            int arrival = _portals.Link(linkId).CellIn(intoModule);
            AddPoint(arrival, path);
            return CastleNavGrid.LocalCell(arrival);
        }

        private bool WalkRoom(int module, int startLocal, int goalLocal, List<Vector3> path)
        {
            if (float.IsInfinity(_gridSearch.Run(_gridSearch.Hop, module, startLocal, goalLocal)))
                return false;
            _gridSearch.AppendPath(_gridSearch.Hop, module, goalLocal, path);
            return true;
        }

        private void AddPoint(int cell, List<Vector3> path)
        {
            Vector3 point = _grid.CellPosition(cell);
            if (path.Count == 0 || path[path.Count - 1] != point)
                path.Add(point);
        }
    }
}

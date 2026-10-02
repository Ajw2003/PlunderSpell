using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// A* over the portal graph: which archways to walk through, in order. The start and goal cells
    /// are not nodes; they enter and leave through the costs the grid floods already measured to each
    /// archway of their own rooms, with the goal as one extra pseudo-node at the end of the arrays.
    /// </summary>
    public sealed class CastleNavPortalSearch
    {
        private const int NoLink = -1;

        private readonly CastleNavPortalGraph _portals;
        private readonly NavMinHeap _heap;
        private readonly float[] _cost;
        private readonly int[] _cameFrom;
        private readonly int[] _walkedThrough;
        private readonly int[] _reachedBy;
        private readonly int[] _closedBy;
        private readonly int[] _route;
        private readonly int[] _routeThrough;
        private readonly int _goalNode;
        private int _search;

        public CastleNavPortalSearch(CastleNavPortalGraph portals, NavMinHeap heap)
        {
            _portals = portals;
            _heap = heap;
            _goalNode = portals.LinkCount;
            int nodes = portals.LinkCount + 1;
            _cost = new float[nodes];
            _cameFrom = new int[nodes];
            _walkedThrough = new int[nodes];
            _reachedBy = new int[nodes];
            _closedBy = new int[nodes];
            _route = new int[nodes];
            _routeThrough = new int[nodes];
        }

        /// <summary>Archways on the route found by the last successful <see cref="Run"/>.</summary>
        public int RouteLength { get; private set; }

        /// <summary>The archway at a step of the route, first step 0.</summary>
        public int RouteLink(int step) => _route[step];

        /// <summary>The room walked through to reach that step's archway.</summary>
        public int RouteModule(int step) => _routeThrough[step];

        /// <summary>
        /// Finds the cheapest chain of archways from the start room to the goal room, given the room
        /// floods from the start and goal cells. Returns false when no chain connects them.
        /// </summary>
        public bool Run(int startModule, int goalModule, Vector3 goalPosition,
            NavSearchBuffer startFlood, NavSearchBuffer goalFlood)
        {
            _search++;
            _heap.Clear();
            for (int end = _portals.EndsBegin(startModule); end < _portals.EndsEnd(startModule); end++)
            {
                float cost = startFlood.CostTo(CastleNavGrid.LocalCell(_portals.End(end).Cell));
                if (!float.IsInfinity(cost))
                    Relax(_portals.End(end).Link, cost, NoLink, startModule, goalPosition);
            }

            while (_heap.Count > 0)
            {
                int node = _heap.Pop();
                if (_closedBy[node] == _search)
                    continue;
                _closedBy[node] = _search;
                if (node == _goalNode)
                {
                    BuildRoute();
                    return true;
                }
                RelaxGoal(node, goalModule, goalFlood, goalPosition);
                for (int edge = _portals.EdgesBegin(node); edge < _portals.EdgesEnd(node); edge++)
                {
                    NavEdge e = _portals.Edge(edge);
                    Relax(e.ToLink, _cost[node] + e.Cost, node, e.ThroughModule, goalPosition);
                }
            }
            return false;
        }

        // An archway of the goal's room finishes the search by adding the room's measured cost to the goal.
        private void RelaxGoal(int link, int goalModule, NavSearchBuffer goalFlood, Vector3 goalPosition)
        {
            for (int end = _portals.EndsBegin(goalModule); end < _portals.EndsEnd(goalModule); end++)
            {
                NavLinkEnd candidate = _portals.End(end);
                if (candidate.Link != link)
                    continue;
                float toGoal = goalFlood.CostTo(CastleNavGrid.LocalCell(candidate.Cell));
                if (!float.IsInfinity(toGoal))
                    Relax(_goalNode, _cost[link] + toGoal, link, goalModule, goalPosition);
            }
        }

        private void Relax(int node, float cost, int cameFrom, int throughModule, Vector3 goalPosition)
        {
            if (_reachedBy[node] == _search && cost >= _cost[node])
                return;
            _reachedBy[node] = _search;
            _cost[node] = cost;
            _cameFrom[node] = cameFrom;
            _walkedThrough[node] = throughModule;
            _heap.Push(cost + Heuristic(node, goalPosition), node);
        }

        // Straight-line distance, less one cell because a link sits on its room-side cell, not the crossing.
        private float Heuristic(int node, Vector3 goalPosition)
        {
            if (node == _goalNode)
                return 0f;
            Vector3 position = _portals.Link(node).Position;
            float dx = position.x - goalPosition.x, dz = position.z - goalPosition.z;
            return Mathf.Max(0f, Mathf.Sqrt(dx * dx + dz * dz) - CastleNavTile.CellSize * 2f);
        }

        private void BuildRoute()
        {
            int length = 0;
            for (int node = _cameFrom[_goalNode]; node != NoLink; node = _cameFrom[node])
                length++;
            RouteLength = length;
            int step = length - 1;
            for (int node = _cameFrom[_goalNode]; node != NoLink; node = _cameFrom[node])
            {
                _route[step] = node;
                _routeThrough[step] = _walkedThrough[node];
                step--;
            }
        }
    }
}

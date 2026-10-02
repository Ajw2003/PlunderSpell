using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// The coarse layer: one node per open archway (<see cref="NavLink"/>), and for each room the cost
    /// of walking between every pair of its archways. Stored as flat arrays with start offsets, so
    /// the portal search walks it without allocating. At most a few hundred nodes, so a search over
    /// it is microseconds.
    /// </summary>
    public sealed class CastleNavPortalGraph
    {
        private readonly NavLink[] _links;
        // Per module, the archways it touches: _endStart[module] up to _endStart[module + 1].
        private readonly int[] _endStart;
        private readonly NavLinkEnd[] _ends;
        // Per link, the costs across the rooms it touches: _edgeStart[link] up to _edgeStart[link + 1].
        private readonly int[] _edgeStart;
        private NavEdge[] _edges;

        public CastleNavPortalGraph(List<NavLink> links, int moduleCount)
        {
            _links = links.ToArray();
            _endStart = new int[moduleCount + 1];
            _ends = new NavLinkEnd[_links.Length * 2];
            CollectEnds(moduleCount);
            _edgeStart = new int[_links.Length + 1];
            _edges = new NavEdge[0];
        }

        public int LinkCount => _links.Length;
        public NavLink Link(int link) => _links[link];
        public int EndsBegin(int module) => _endStart[module];
        public int EndsEnd(int module) => _endStart[module + 1];
        public NavLinkEnd End(int index) => _ends[index];
        public int EdgesBegin(int link) => _edgeStart[link];
        public int EdgesEnd(int link) => _edgeStart[link + 1];
        public NavEdge Edge(int index) => _edges[index];

        private void CollectEnds(int moduleCount)
        {
            foreach (NavLink link in _links)
            {
                _endStart[link.ModuleA + 1]++;
                _endStart[link.ModuleB + 1]++;
            }
            for (int module = 0; module < moduleCount; module++)
                _endStart[module + 1] += _endStart[module];
            var filled = new int[moduleCount];
            for (int i = 0; i < _links.Length; i++)
            {
                _ends[_endStart[_links[i].ModuleA] + filled[_links[i].ModuleA]++] = new NavLinkEnd(i, _links[i].CellA);
                _ends[_endStart[_links[i].ModuleB] + filled[_links[i].ModuleB]++] = new NavLinkEnd(i, _links[i].CellB);
            }
        }

        /// <summary>
        /// Floods each room from each of its archways to price the walk to every other archway of
        /// that room. Unreachable pairs (a gallery cut off from the door) get no edge.
        /// </summary>
        public void ComputeRoomCrossings(CastleNavGridSearch search, int moduleCount)
        {
            var perLink = new List<NavEdge>[_links.Length];
            for (int i = 0; i < perLink.Length; i++)
                perLink[i] = new List<NavEdge>();

            for (int module = 0; module < moduleCount; module++)
            {
                for (int from = _endStart[module]; from < _endStart[module + 1]; from++)
                    PriceCrossingsFrom(search, module, from, perLink);
            }
            Flatten(perLink);
        }

        private void PriceCrossingsFrom(CastleNavGridSearch search, int module, int fromEnd, List<NavEdge>[] perLink)
        {
            search.Run(search.Hop, module, CastleNavGrid.LocalCell(_ends[fromEnd].Cell), -1);
            for (int to = _endStart[module]; to < _endStart[module + 1]; to++)
            {
                if (to == fromEnd)
                    continue;
                float cost = search.Hop.CostTo(CastleNavGrid.LocalCell(_ends[to].Cell));
                if (!float.IsInfinity(cost))
                    perLink[_ends[fromEnd].Link].Add(new NavEdge(_ends[to].Link, module, cost));
            }
        }

        private void Flatten(List<NavEdge>[] perLink)
        {
            int total = 0;
            for (int link = 0; link < perLink.Length; link++)
            {
                _edgeStart[link] = total;
                total += perLink[link].Count;
            }
            _edgeStart[perLink.Length] = total;
            _edges = new NavEdge[total];
            for (int link = 0; link < perLink.Length; link++)
                perLink[link].CopyTo(_edges, _edgeStart[link]);
        }

        /// <summary>Order-sensitive hash of the links and costs, for comparing two builds.</summary>
        public int Checksum()
        {
            unchecked
            {
                int hash = 17;
                foreach (NavLink link in _links)
                    hash = hash * 31 + link.CellA * 3 + link.CellB;
                foreach (NavEdge edge in _edges)
                    hash = hash * 31 + edge.ToLink + Mathf.RoundToInt(edge.Cost * 100f);
                return hash;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Straightens a cell-by-cell path. The graph hands back every 0.5 m cell centre in a 4-connected
    /// chain, which would make a guard walk in staircases; this keeps only the cells where the route
    /// really has to turn. From each kept point it looks ahead along the chain for the farthest cell
    /// it can walk to in a straight line, and stops looking at the first one it cannot.
    /// </summary>
    public sealed class GuardPathSmoother
    {
        private readonly List<Vector3> _kept = new List<Vector3>();

        /// <summary>The corner points of the last <see cref="Smooth"/>, valid until the next call.</summary>
        public List<Vector3> Kept => _kept;

        public void Smooth(List<Vector3> cellChain, IGuardNavigationMap map, float halfWidth)
        {
            _kept.Clear();
            if (cellChain.Count == 0)
                return;

            int anchor = 0;
            _kept.Add(cellChain[0]);
            while (anchor < cellChain.Count - 1)
            {
                anchor = FarthestVisibleFrom(anchor, cellChain, map, halfWidth);
                _kept.Add(cellChain[anchor]);
            }
        }

        // The next chain cell is always allowed, since it is one adjacent step; beyond it a cell must
        // be in a clear straight line, and the first cell that is not ends the search (a later one
        // might be visible again, but only by cutting a corner the chain went round on purpose).
        private static int FarthestVisibleFrom(int anchor, List<Vector3> cellChain, IGuardNavigationMap map, float halfWidth)
        {
            int farthest = anchor + 1;
            for (int candidate = anchor + 2; candidate < cellChain.Count; candidate++)
            {
                if (!map.IsWalkClear(cellChain[anchor], cellChain[candidate], halfWidth))
                    break;
                farthest = candidate;
            }
            return farthest;
        }
    }
}

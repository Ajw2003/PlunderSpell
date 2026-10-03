using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// One open archway between two rooms: a node of the coarse portal graph. The two cells are the
    /// archway's middle floor cell as seen from each room; they are neighbours across the doorway.
    /// </summary>
    public readonly struct NavLink
    {
        public readonly int ModuleA;
        public readonly int ModuleB;
        public readonly int CellA;
        public readonly int CellB;
        /// <summary>World position of <see cref="CellA"/>, for the portal search's heuristic.</summary>
        public readonly Vector3 Position;

        public NavLink(int moduleA, int moduleB, int cellA, int cellB, Vector3 position)
        {
            ModuleA = moduleA;
            ModuleB = moduleB;
            CellA = cellA;
            CellB = cellB;
            Position = position;
        }

        /// <summary>The archway's cell on the given room's side.</summary>
        public int CellIn(int module)
        {
            return ModuleA == module ? CellA : CellB;
        }
    }
}

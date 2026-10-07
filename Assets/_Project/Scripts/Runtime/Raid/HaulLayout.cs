using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Where each piece of the haul lies on the Lair floor: a grid so pieces do not overlap, filled
    /// from the centre outward so a small haul lies straight ahead of the arriving players, stacked in
    /// layers if there are more pieces than cells.
    /// </summary>
    public static class HaulLayout
    {
        public const float Spacing = 0.45f;
        public const int Columns = 7;
        public const int Rows = 3;
        public const float LayerHeight = 0.4f;

        /// <summary>Offsets from the pad's centre: columns along Z, rows along X, layers up.</summary>
        public static List<Vector3> Offsets(int count)
        {
            List<Vector2> cells = CellsCentreFirst();
            var offsets = new List<Vector3>(count);
            for (int i = 0; i < count; i++)
            {
                Vector2 cell = cells[i % cells.Count];
                offsets.Add(new Vector3(cell.y * Spacing, i / cells.Count * LayerHeight, cell.x * Spacing));
            }
            return offsets;
        }

        /// <summary>Every (column, row) cell, centred on zero, nearest the centre first.</summary>
        private static List<Vector2> CellsCentreFirst()
        {
            var cells = new List<Vector2>(Columns * Rows);
            for (int row = 0; row < Rows; row++)
                for (int column = 0; column < Columns; column++)
                    cells.Add(new Vector2(column - (Columns - 1) * 0.5f, row - (Rows - 1) * 0.5f));
            // OrderBy is stable, so equal distances keep authored order and the layout is the same every time.
            return cells.OrderBy(c => c.sqrMagnitude).ToList();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Where each piece of the haul lies on the Lair floor: a grid so pieces do not overlap, filled
    /// row by row, stacked in layers if there are more pieces than cells.
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
            var offsets = new List<Vector3>(count);
            int perLayer = Columns * Rows;
            for (int i = 0; i < count; i++)
            {
                int cell = i % perLayer;
                float column = cell % Columns - (Columns - 1) * 0.5f;
                float row = cell / Columns - (Rows - 1) * 0.5f;
                offsets.Add(new Vector3(row * Spacing, i / perLayer * LayerHeight, column * Spacing));
            }
            return offsets;
        }
    }
}

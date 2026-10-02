using System;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// A room module's walkable grid, baked once in the editor by Tools/Plunderspell/Bake Castle Nav
    /// Tiles. 24 x 24 cells of 0.5 m in module-local space (module centred on its origin, so cell
    /// (0,0) covers x,z in [-6,-5.5]). Each cell column holds up to <see cref="Layers"/> stacked
    /// walkable surfaces (layer 0 lowest), so a gallery over a floor is two layers of one column.
    /// Stairs are walkable cells whose heights step up; two cells connect when their heights differ
    /// by at most <see cref="StepHeight"/>. Plain arrays, so reading allocates nothing.
    /// </summary>
    [Serializable]
    public class CastleNavTile
    {
        /// <summary>Cells along each side.</summary>
        public const int Size = 24;
        /// <summary>Metres per cell.</summary>
        public const float CellSize = 0.5f;
        /// <summary>Stacked surfaces kept per cell column.</summary>
        public const int Layers = 2;
        /// <summary>Largest height difference between connected cells, metres.</summary>
        public const float StepHeight = 0.45f;
        /// <summary>Height units per metre: heights are stored as centimetres in a short.</summary>
        public const float HeightScale = 100f;

        /// <summary>Per (layer, cell): 1 when a guard can stand there. Index via <see cref="Index"/>.</summary>
        public byte[] Walkable = new byte[0];

        /// <summary>Per (layer, cell): floor height in centimetres, module-local. Meaningless where not walkable.</summary>
        public short[] HeightCm = new short[0];

        /// <summary>Cell indices (z * Size + x) of the archway opening on the north (+Z) side.</summary>
        public ushort[] PortalNorth = new ushort[0];
        /// <summary>Cell indices of the archway opening on the east (+X) side.</summary>
        public ushort[] PortalEast = new ushort[0];
        /// <summary>Cell indices of the archway opening on the south (-Z) side.</summary>
        public ushort[] PortalSouth = new ushort[0];
        /// <summary>Cell indices of the archway opening on the west (-X) side.</summary>
        public ushort[] PortalWest = new ushort[0];

        /// <summary>How many layers hold at least one walkable cell.</summary>
        public int LevelCount;

        /// <summary>True once baked.</summary>
        public bool IsBaked => Walkable != null && Walkable.Length == Layers * Size * Size;

        /// <summary>Array index for a layer and cell.</summary>
        public static int Index(int layer, int x, int z)
        {
            return (layer * Size + z) * Size + x;
        }

        /// <summary>True when the surface at (layer, x, z) is walkable.</summary>
        public bool IsWalkable(int layer, int x, int z)
        {
            return Walkable[Index(layer, x, z)] != 0;
        }

        /// <summary>Floor height in metres, module-local, of the surface at (layer, x, z).</summary>
        public float Height(int layer, int x, int z)
        {
            return HeightCm[Index(layer, x, z)] / HeightScale;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// The fine layer: every placed module's walkable cells, already turned to the module's world
    /// orientation. A cell id is <c>module * CellsPerModule + layer * ColumnsPerModule + row * TileSize
    /// + column</c>; the part after the module is the "room-local" cell. The hot per-cell data is
    /// three flat arrays (walkable, height, area) so reads allocate nothing and stay cache friendly.
    /// Also owns the step rule: two cells connect when both are walkable and their floors are no more
    /// than one step apart.
    /// </summary>
    public sealed class CastleNavGrid
    {
        /// <summary>Cells along each side of a module.</summary>
        public const int TileSize = CastleNavTile.Size;
        /// <summary>Cells in one layer of a module.</summary>
        public const int ColumnsPerModule = TileSize * TileSize;
        /// <summary>Cells in all layers of a module.</summary>
        public const int CellsPerModule = CastleNavTile.Layers * ColumnsPerModule;
        /// <summary>Returned when there is no such cell.</summary>
        public const int NoCell = -1;

        private const float ModuleSpan = TileSize * CastleNavTile.CellSize;
        private const float HalfSpan = ModuleSpan * 0.5f;
        private static readonly int s_stepHeightCm = Mathf.RoundToInt(CastleNavTile.StepHeight * CastleNavTile.HeightScale);

        // Which way a module's neighbour lies per side: north (+Z), east (+X), south (-Z), west (-X).
        private static readonly int[] s_sideColumnOffset = { 0, 1, 0, -1 };
        private static readonly int[] s_sideRowOffset = { 1, 0, -1, 0 };
        // The four cell-to-cell steps inside a layer.
        private static readonly int[] s_stepColumn = { 1, -1, 0, 0 };
        private static readonly int[] s_stepRow = { 0, 0, 1, -1 };

        private readonly Vector3[] _modulePosition;
        private readonly bool[] _moduleHasTile;
        private readonly byte[] _walkable;
        private readonly short[] _heightCm;
        private readonly int[] _areaId;
        // Archway cells per module and world side, by index along that side's edge.
        private readonly bool[] _archway;
        private readonly Vector2Int _lookupOrigin;
        private readonly int _lookupWidth;
        private readonly int _lookupHeight;
        private readonly int[] _moduleAtGridCell;

        /// <summary>Modules in the layout, walkable or not.</summary>
        public int ModuleCount { get; }

        /// <summary>False when the modules are not laid out one span apart, so cell lookups would be wrong.</summary>
        public bool IsUsable { get; }

        /// <summary>Walkable cells over the whole castle.</summary>
        public int WalkableCellCount { get; private set; }

        public CastleNavGrid(List<ProceduralCastleData.PlacedModule> placed)
        {
            ModuleCount = placed.Count;
            _modulePosition = new Vector3[ModuleCount];
            _moduleHasTile = new bool[ModuleCount];
            _walkable = new byte[ModuleCount * CellsPerModule];
            _heightCm = new short[ModuleCount * CellsPerModule];
            _areaId = new int[ModuleCount * CellsPerModule];
            _archway = new bool[ModuleCount * 4 * TileSize];
            for (int cell = 0; cell < _areaId.Length; cell++)
                _areaId[cell] = -1;

            Vector2Int min = placed[0].GridPosition, max = min;
            for (int module = 0; module < ModuleCount; module++)
            {
                min = Vector2Int.Min(min, placed[module].GridPosition);
                max = Vector2Int.Max(max, placed[module].GridPosition);
            }
            _lookupOrigin = min;
            _lookupWidth = max.x - min.x + 1;
            _lookupHeight = max.y - min.y + 1;
            _moduleAtGridCell = new int[_lookupWidth * _lookupHeight];
            for (int i = 0; i < _moduleAtGridCell.Length; i++)
                _moduleAtGridCell[i] = -1;
            IsUsable = RegisterModules(placed);
        }

        // Cell lookups go by grid cell and assume neighbours sit exactly one span apart.
        private bool RegisterModules(List<ProceduralCastleData.PlacedModule> placed)
        {
            for (int module = 0; module < ModuleCount; module++)
            {
                ProceduralCastleData.PlacedModule pm = placed[module];
                _modulePosition[module] = pm.Position;
                _moduleAtGridCell[(pm.GridPosition.y - _lookupOrigin.y) * _lookupWidth + (pm.GridPosition.x - _lookupOrigin.x)] = module;
                if (Mathf.Abs(pm.Position.x - pm.GridPosition.x * ModuleSpan) > 0.01f ||
                    Mathf.Abs(pm.Position.z - pm.GridPosition.y * ModuleSpan) > 0.01f)
                {
                    Debug.LogWarning($"[NavGraph] Module {pm.RoomId} at {pm.GridPosition} is not {ModuleSpan} m from its neighbours; nav graph left empty.");
                    return false;
                }
            }
            return true;
        }

        // --- Loading (used by the stitcher) ---------------------------------------------------

        /// <summary>Marks a module as having a baked tile loaded.</summary>
        public void MarkTile(int module)
        {
            _moduleHasTile[module] = true;
        }

        /// <summary>Makes a cell walkable at a floor height.</summary>
        public void SetWalkable(int cell, short heightCm)
        {
            _walkable[cell] = 1;
            _heightCm[cell] = heightCm;
            WalkableCellCount++;
        }

        /// <summary>Marks the cell at <paramref name="along"/> on a module's world-side edge as an archway cell.</summary>
        public void SetArchway(int module, int side, int along)
        {
            _archway[(module * 4 + side) * TileSize + along] = true;
        }

        /// <summary>Records which connected area a cell belongs to.</summary>
        public void SetAreaId(int cell, int areaId)
        {
            _areaId[cell] = areaId;
        }

        // --- Reading --------------------------------------------------------------------------

        public bool HasTile(int module) => _moduleHasTile[module];
        public bool IsWalkable(int cell) => _walkable[cell] != 0;
        public int HeightCm(int cell) => _heightCm[cell];
        public int AreaId(int cell) => _areaId[cell];
        public bool IsArchway(int module, int side, int along) => _archway[(module * 4 + side) * TileSize + along];
        public static int ModuleOf(int cell) => cell / CellsPerModule;
        public static int LocalCell(int cell) => cell % CellsPerModule;

        /// <summary>The module placed at a grid cell, or -1.</summary>
        public int ModuleAtGridCell(int gridX, int gridY)
        {
            int x = gridX - _lookupOrigin.x, y = gridY - _lookupOrigin.y;
            if (x < 0 || y < 0 || x >= _lookupWidth || y >= _lookupHeight)
                return -1;
            return _moduleAtGridCell[y * _lookupWidth + x];
        }

        /// <summary>Grid offset of the neighbour across a world side (0 north, 1 east, 2 south, 3 west).</summary>
        public static Vector2Int SideOffset(int side) => new Vector2Int(s_sideColumnOffset[side], s_sideRowOffset[side]);

        /// <summary>The layer-0 cell on a module's edge, <paramref name="along"/> cells along that side.</summary>
        public static int EdgeCell(int module, int side, int along)
        {
            int column, row;
            switch (side)
            {
                case 0: column = along; row = TileSize - 1; break;
                case 1: column = TileSize - 1; row = along; break;
                case 2: column = along; row = 0; break;
                default: column = 0; row = along; break;
            }
            return module * CellsPerModule + row * TileSize + column;
        }

        /// <summary>True when walking from a cell to another floor height is no more than one step.</summary>
        public bool IsStep(int cellA, int cellB)
        {
            return Math.Abs(_heightCm[cellA] - _heightCm[cellB]) <= s_stepHeightCm;
        }

        // --- Steps inside a room ----------------------------------------------------------------

        /// <summary>
        /// The cell one step in <paramref name="direction"/> on <paramref name="layer"/>, as a
        /// room-local cell, if it is in the same module, walkable, and no more than a step apart.
        /// </summary>
        public bool TryStep(int module, int localCell, int direction, int layer, out int neighbour)
        {
            int column = localCell % TileSize + s_stepColumn[direction];
            int row = localCell / TileSize % TileSize + s_stepRow[direction];
            neighbour = 0;
            if (column < 0 || row < 0 || column >= TileSize || row >= TileSize)
                return false;
            neighbour = layer * ColumnsPerModule + row * TileSize + column;
            int baseCell = module * CellsPerModule;
            return _walkable[baseCell + neighbour] != 0 && IsStep(baseCell + neighbour, baseCell + localCell);
        }

        /// <summary>Walking cost of one step between neighbouring cells: a cell's width, plus the climb.</summary>
        public float StepCost(int cellA, int cellB)
        {
            return CastleNavTile.CellSize + Math.Abs(_heightCm[cellA] - _heightCm[cellB]) / CastleNavTile.HeightScale;
        }

        // --- Positions ---------------------------------------------------------------------------

        /// <summary>World position of a cell's centre at its floor height.</summary>
        public Vector3 CellPosition(int cell)
        {
            int local = LocalCell(cell);
            Vector3 origin = _modulePosition[ModuleOf(cell)];
            return new Vector3(origin.x - HalfSpan + (local % TileSize + 0.5f) * CastleNavTile.CellSize,
                _heightCm[cell] / CastleNavTile.HeightScale,
                origin.z - HalfSpan + (local / TileSize % TileSize + 0.5f) * CastleNavTile.CellSize);
        }

        /// <summary>
        /// The walkable cell nearest a world point by 3D distance (so a gallery and the floor under
        /// it are told apart), or <see cref="NoCell"/> when none lies within <paramref name="maxDistance"/>.
        /// </summary>
        public int NearestWalkableCell(Vector3 position, float maxDistance)
        {
            if (ModuleCount == 0)
                return NoCell;
            int reach = Mathf.CeilToInt(maxDistance / CastleNavTile.CellSize);
            int centreColumn = Mathf.FloorToInt((position.x + HalfSpan) / CastleNavTile.CellSize);
            int centreRow = Mathf.FloorToInt((position.z + HalfSpan) / CastleNavTile.CellSize);
            int best = NoCell;
            float bestSqr = maxDistance * maxDistance;
            for (int dr = -reach; dr <= reach; dr++)
            {
                for (int dc = -reach; dc <= reach; dc++)
                    ConsiderColumn(position, centreColumn + dc, centreRow + dr, ref best, ref bestSqr);
            }
            return best;
        }

        // A column is addressed by its castle-wide cell coordinates, which may fall in a neighbour module.
        private void ConsiderColumn(Vector3 position, int castleColumn, int castleRow, ref int best, ref float bestSqr)
        {
            int gridX = FloorDiv(castleColumn, TileSize), gridY = FloorDiv(castleRow, TileSize);
            int module = ModuleAtGridCell(gridX, gridY);
            if (module < 0 || !_moduleHasTile[module])
                return;
            int column = castleColumn - gridX * TileSize, row = castleRow - gridY * TileSize;
            float dx = _modulePosition[module].x - HalfSpan + (column + 0.5f) * CastleNavTile.CellSize - position.x;
            float dz = _modulePosition[module].z - HalfSpan + (row + 0.5f) * CastleNavTile.CellSize - position.z;
            for (int layer = 0; layer < CastleNavTile.Layers; layer++)
            {
                int cell = module * CellsPerModule + CastleNavTile.Index(layer, column, row);
                if (_walkable[cell] == 0)
                    continue;
                float dy = _heightCm[cell] / CastleNavTile.HeightScale - position.y;
                float sqr = dx * dx + dz * dz + dy * dy;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = cell;
                }
            }
        }

        private static int FloorDiv(int numerator, int denominator)
        {
            int quotient = numerator / denominator;
            return numerator % denominator < 0 ? quotient - 1 : quotient;
        }

        /// <summary>Order-sensitive hash of the cells, for comparing two builds.</summary>
        public int Checksum()
        {
            unchecked
            {
                int hash = 17;
                for (int cell = 0; cell < _walkable.Length; cell++)
                    hash = hash * 31 + _walkable[cell] + _heightCm[cell] * 7 + _areaId[cell] * 13;
                return hash;
            }
        }
    }
}

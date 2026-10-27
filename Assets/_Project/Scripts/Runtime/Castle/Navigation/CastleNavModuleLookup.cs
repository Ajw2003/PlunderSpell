using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Which module stands on a grid cell at a storey. A stair is entered at every storey it spans, so
    /// stacked rooms and the stairs between them are each found on the floor the caller asks about.
    /// </summary>
    public sealed class CastleNavModuleLookup
    {
        private readonly Vector2Int _origin;
        private readonly int _width;
        private readonly int _height;
        private readonly int[] _moduleAt;

        public CastleNavModuleLookup(List<ProceduralCastleData.PlacedModule> placed)
        {
            Vector2Int min = placed[0].GridPosition, max = min;
            foreach (ProceduralCastleData.PlacedModule module in placed)
            {
                min = Vector2Int.Min(min, module.GridPosition);
                max = Vector2Int.Max(max, module.GridPosition);
            }
            _origin = min;
            _width = max.x - min.x + 1;
            _height = max.y - min.y + 1;
            _moduleAt = new int[_width * _height * CastleLevels.Count];
            for (int i = 0; i < _moduleAt.Length; i++)
                _moduleAt[i] = -1;
        }

        /// <summary>Records a module at every storey it spans.</summary>
        public void Register(int module, ProceduralCastleData.PlacedModule placed)
        {
            for (int level = placed.Level; level <= placed.TopLevel; level++)
                _moduleAt[Index(placed.GridPosition.x - _origin.x, placed.GridPosition.y - _origin.y, level)] = module;
        }

        /// <summary>The module placed at a grid cell on a storey, or -1.</summary>
        public int At(int gridX, int gridY, int level)
        {
            int x = gridX - _origin.x, y = gridY - _origin.y;
            int index = CastleLevels.Index(level);
            if (x < 0 || y < 0 || x >= _width || y >= _height || index < 0 || index >= CastleLevels.Count)
                return -1;
            return _moduleAt[Index(x, y, level)];
        }

        private int Index(int x, int y, int level) => (CastleLevels.Index(level) * _height + y) * _width + x;
    }
}

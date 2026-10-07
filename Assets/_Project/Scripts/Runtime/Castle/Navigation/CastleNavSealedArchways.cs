namespace Plunderspell.Castle
{
    /// <summary>
    /// Takes the walk map out of every archway the generator walled off with a door plug. The plug's
    /// collider sits on the edge cells, so leaving them walkable sends guards to stand inside a wall
    /// (a keep stairwell whose foot faces a sealed side stopped guards with "Obstacle", #255).
    /// Stairs are left alone: their walled storey is closed in the prefab and their layers span two levels.
    /// </summary>
    public static class CastleNavSealedArchways
    {
        public static void Clear(CastleNavGrid grid, ProceduralCastleData data)
        {
            for (int module = 0; module < grid.ModuleCount; module++)
            {
                ProceduralCastleData.PlacedModule placed = data.PlacedModules[module];
                if (!grid.HasTile(module) || !ProceduralCastleGenerator.IsEnclosedRoom(placed.Zone) || placed.Storeys > 1)
                    continue;
                for (int side = 0; side < 4; side++)
                {
                    var at = placed.GridPosition + CastleNavGrid.SideOffset(side);
                    int other = grid.ModuleAtGridCell(at.x, at.y, placed.Level);
                    // Mirrors the generator's SealOpenArchways, which plugs what IsOpen says is closed.
                    if (other >= 0 && CastleNavArchwayRule.IsOpen(data, placed, data.PlacedModules[other], placed.Level))
                        continue;
                    ClearEdge(grid, module, side);
                }
            }
        }

        private static void ClearEdge(CastleNavGrid grid, int module, int side)
        {
            for (int along = 0; along < CastleNavGrid.TileSize; along++)
            {
                if (!grid.IsArchway(module, side, along))
                    continue;
                for (int layer = 0; layer < CastleNavTile.Layers; layer++)
                    grid.ClearWalkable(CastleNavGrid.EdgeCell(module, side, along) + layer * CastleNavGrid.ColumnsPerModule);
            }
        }
    }
}

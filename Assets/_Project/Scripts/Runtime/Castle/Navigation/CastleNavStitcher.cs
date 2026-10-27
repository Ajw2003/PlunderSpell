using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Stitches the placed modules into one graph: turns each module's baked tile to the way the
    /// module was placed, then joins neighbouring rooms through the archways the generator left open.
    /// Run in the same pass that places the modules, so it sees the same layout on every peer.
    /// </summary>
    public static class CastleNavStitcher
    {
        /// <summary>Copies every placed module's baked tile into the grid, rotated to its placement.</summary>
        public static void LoadTiles(CastleNavGrid grid, ProceduralCastleData data, CastleRoomRegistry registry)
        {
            int unbaked = 0;
            string firstUnbaked = null;
            for (int module = 0; module < grid.ModuleCount; module++)
            {
                ProceduralCastleData.PlacedModule placed = data.PlacedModules[module];
                CastleRoomModuleData entry = registry.GetById(placed.RoomId);
                if (entry == null || entry.NavTile == null || !entry.NavTile.IsBaked)
                {
                    unbaked++;
                    firstUnbaked ??= placed.RoomId;
                    continue;
                }
                grid.MarkTile(module);
                int turns = QuarterTurns(placed.Rotation);
                // Tile heights are module-local; a keep or crypt module stands at its own root height.
                CopyCells(grid, module, entry.NavTile, turns,
                    (short)Mathf.RoundToInt(placed.Position.y * CastleNavTile.HeightScale));
                CopyArchways(grid, module, entry.NavTile.PortalNorth, 0, turns);
                CopyArchways(grid, module, entry.NavTile.PortalEast, 1, turns);
                CopyArchways(grid, module, entry.NavTile.PortalSouth, 2, turns);
                CopyArchways(grid, module, entry.NavTile.PortalWest, 3, turns);
            }

            // A room with no tile is a hole guards cannot stand in or cross; with a whole registry unbaked the
            // map is empty and no guard moves at all, so this must never pass quietly.
            if (unbaked > 0)
                Debug.LogWarning($"[CastleNav] {unbaked} of {grid.ModuleCount} placed rooms have no baked nav tile " +
                                 $"in {registry.name} (first: {firstUnbaked}). Guards cannot walk there. " +
                                 "Run Tools/Plunderspell/Bake Castle Nav Tiles.");
        }

        // Modules are only ever turned in quarters about the vertical axis.
        private static int QuarterTurns(Quaternion rotation)
        {
            int turns = Mathf.RoundToInt(rotation.eulerAngles.y / 90f) % 4;
            return turns < 0 ? turns + 4 : turns;
        }

        // A +90 degree yaw takes cell (column, row) to (row, last - column) and turns north into east.
        private static void Rotate(int turns, ref int column, ref int row)
        {
            for (int turn = 0; turn < turns; turn++)
            {
                int turnedColumn = row;
                row = CastleNavGrid.TileSize - 1 - column;
                column = turnedColumn;
            }
        }

        private static void CopyCells(CastleNavGrid grid, int module, CastleNavTile tile, int turns, short offsetCm)
        {
            for (int layer = 0; layer < CastleNavTile.Layers; layer++)
            {
                for (int row = 0; row < CastleNavGrid.TileSize; row++)
                {
                    for (int column = 0; column < CastleNavGrid.TileSize; column++)
                    {
                        int source = CastleNavTile.Index(layer, column, row);
                        if (tile.Walkable[source] == 0)
                            continue;
                        int turnedColumn = column, turnedRow = row;
                        Rotate(turns, ref turnedColumn, ref turnedRow);
                        grid.SetWalkable(module * CastleNavGrid.CellsPerModule + CastleNavTile.Index(layer, turnedColumn, turnedRow),
                            (short)(tile.HeightCm[source] + offsetCm));
                    }
                }
            }
        }

        private static void CopyArchways(CastleNavGrid grid, int module, ushort[] cells, int tileSide, int turns)
        {
            int worldSide = (tileSide + turns) % 4;
            foreach (ushort cell in cells)
            {
                int column = cell % CastleNavGrid.TileSize, row = cell / CastleNavGrid.TileSize;
                Rotate(turns, ref column, ref row);
                grid.SetArchway(module, worldSide, worldSide == 0 || worldSide == 2 ? column : row);
            }
        }

        /// <summary>
        /// Finds every open archway between neighbouring modules, one link each, and joins the areas
        /// of the cells they connect. Each pair of neighbours is looked at once, from the lower index.
        /// </summary>
        public static List<NavLink> FindLinks(CastleNavGrid grid, ProceduralCastleData data, CastleNavAreas areas)
        {
            var links = new List<NavLink>();
            for (int module = 0; module < grid.ModuleCount; module++)
            {
                if (!grid.HasTile(module))
                    continue;
                ProceduralCastleData.PlacedModule placed = data.PlacedModules[module];
                for (int level = placed.Level; level <= placed.TopLevel; level++)
                {
                    for (int side = 0; side < 4; side++)
                    {
                        Vector2Int at = placed.GridPosition + CastleNavGrid.SideOffset(side);
                        int other = grid.ModuleAtGridCell(at.x, at.y, level);
                        // Each pair is looked at once, from the lower index: no two multi-storey modules
                        // sit side by side on two shared levels in this layout, so a pair meets on one level only.
                        if (other <= module || !grid.HasTile(other) ||
                            !CastleNavArchwayRule.IsOpen(data, placed, data.PlacedModules[other], level))
                            continue;
                        if (TryJoin(grid, areas, module, side, other, out NavLink link))
                            links.Add(link);
                    }
                }
            }
            return links;
        }

        // Joins every archway cell pair across the shared edge; the link sits on the pair nearest the middle.
        private static bool TryJoin(CastleNavGrid grid, CastleNavAreas areas, int module, int side, int other, out NavLink link)
        {
            int otherSide = (side + 2) % 4;
            int linkCell = -1, linkAcross = -1;
            float middleOffset = float.MaxValue;
            for (int along = 0; along < CastleNavGrid.TileSize; along++)
            {
                if (!grid.IsArchway(module, side, along) || !grid.IsArchway(other, otherSide, along))
                    continue;
                float offset = Mathf.Abs(along - (CastleNavGrid.TileSize - 1) * 0.5f);
                // Every layer pair, so a stair head (layer 1) meets a keep room (layer 0).
                for (int la = 0; la < CastleNavTile.Layers; la++)
                {
                    for (int lb = 0; lb < CastleNavTile.Layers; lb++)
                    {
                        int cell = CastleNavGrid.EdgeCell(module, side, along) + la * CastleNavGrid.ColumnsPerModule;
                        int across = CastleNavGrid.EdgeCell(other, otherSide, along) + lb * CastleNavGrid.ColumnsPerModule;
                        if (!grid.IsWalkable(cell) || !grid.IsWalkable(across) || !grid.IsStep(cell, across))
                            continue;
                        areas.Join(grid, cell, across);
                        if (offset < middleOffset)
                        {
                            middleOffset = offset;
                            linkCell = cell;
                            linkAcross = across;
                        }
                    }
                }
            }
            link = default;
            if (linkCell < 0)
                return false;
            link = new NavLink(module, other, linkCell, linkAcross, grid.CellPosition(linkCell));
            return true;
        }
    }
}

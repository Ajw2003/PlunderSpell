using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    // doc-ref f151 docs/4-systems/castle.md
    /// <summary>
    /// Which cells of the curtain-wall strip open into the room inward of them (#140). Pure, so
    /// every peer and every test derives the same entrances from the layout.
    /// </summary>
    public static class CastleEntrancePlanner
    {
        /// <summary>
        /// The curtain-wall cells that are entrances, in layout order.
        /// <paramref name="straightWallIds"/> are the room ids of a plain run of curtain wall.
        /// </summary>
        public static List<Vector2Int> Plan(ProceduralCastleData castle, int curtainWallRadius,
            ICollection<string> straightWallIds)
        {
            var entrances = new List<Vector2Int>();
            if (castle?.PlacedModules == null)
                return entrances;

            // A side whose even slots are all bastions or face courtyards falls back to its
            // qualifying cell nearest the middle, so no side of the strip is a dead end.
            var fallbackBySide = new Dictionary<Vector2Int, Vector2Int>();
            var sidesWithEntrance = new HashSet<Vector2Int>();

            var zoneByCell = new Dictionary<Vector2Int, CastleZone>();
            foreach (ProceduralCastleData.PlacedModule module in castle.PlacedModules)
            {
                // Entrances open onto the ground floor; keep and crypt rooms share these cells (#247).
                if (module.Level == CastleLevels.Ground)
                    zoneByCell[module.GridPosition] = module.Zone;
            }

            for (int i = 0; i < castle.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = castle.PlacedModules[i];
                if (module.Zone != CastleZone.CurtainWall || module.IsExtractionExit || i == castle.ExtractionExitIndex)
                    continue;
                if (!straightWallIds.Contains(module.RoomId))
                    continue;
                Vector2Int cell = module.GridPosition;
                if (!IsSideCell(cell, curtainWallRadius))
                    continue;
                if (!zoneByCell.TryGetValue(InwardCell(cell), out CastleZone inward)
                    || !ProceduralCastleGenerator.IsEnclosedRoom(inward))
                    continue;

                Vector2Int side = SideOf(cell, curtainWallRadius);
                if (IsEntranceSlot(cell, curtainWallRadius))
                {
                    entrances.Add(cell);
                    sidesWithEntrance.Add(side);
                }
                else if (!fallbackBySide.TryGetValue(side, out Vector2Int best)
                         || Mathf.Abs(Along(cell, curtainWallRadius)) < Mathf.Abs(Along(best, curtainWallRadius)))
                {
                    fallbackBySide[side] = cell;
                }
            }

            foreach (KeyValuePair<Vector2Int, Vector2Int> fallback in fallbackBySide)
                if (!sidesWithEntrance.Contains(fallback.Key))
                    entrances.Add(fallback.Value);
            return entrances;
        }

        /// <summary>True for a ring cell that is not a corner.</summary>
        public static bool IsSideCell(Vector2Int cell, int curtainWallRadius) =>
            (Mathf.Abs(cell.x) == curtainWallRadius) != (Mathf.Abs(cell.y) == curtainWallRadius);

        /// <summary>Which side of the ring a side cell is on, as an outward unit step.</summary>
        public static Vector2Int SideOf(Vector2Int cell, int curtainWallRadius) =>
            Mathf.Abs(cell.x) == curtainWallRadius
                ? new Vector2Int(System.Math.Sign(cell.x), 0)
                : new Vector2Int(0, System.Math.Sign(cell.y));

        private static int Along(Vector2Int cell, int curtainWallRadius) =>
            Mathf.Abs(cell.x) == curtainWallRadius ? cell.y : cell.x;

        /// <summary>True for a side cell of the curtain ring at an even position along its side,
        /// which spaces entrances two cells apart. Corners never qualify. A side with none of these
        /// usable gets one other cell instead.</summary>
        public static bool IsEntranceSlot(Vector2Int cell, int curtainWallRadius) =>
            IsSideCell(cell, curtainWallRadius) && Along(cell, curtainWallRadius) % 2 == 0;

        /// <summary>The cell one step toward the castle's centre from a ring cell.</summary>
        public static Vector2Int InwardCell(Vector2Int cell)
        {
            if (Mathf.Abs(cell.x) >= Mathf.Abs(cell.y))
                return new Vector2Int(cell.x - System.Math.Sign(cell.x), cell.y);
            return new Vector2Int(cell.x, cell.y - System.Math.Sign(cell.y));
        }
    }
}

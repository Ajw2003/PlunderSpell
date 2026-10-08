using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Which ways a module opens on a level (#247). Rooms open on all four sides. A stair opens like a room on
    /// its lobby level, and only toward <see cref="ProceduralCastleData.PlacedModule.ExitFacing"/> on its exit
    /// level, so each stair is one door's width at the zone change.
    /// </summary>
    public static class CastleStairRule
    {
        public static bool Opens(ProceduralCastleData.PlacedModule module, int level, Vector2Int toward)
        {
            if (module.Storeys <= 1 || level != module.ExitLevel)
                return true;
            return toward == module.ExitFacing;
        }
    }
}

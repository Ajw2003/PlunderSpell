using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>
    /// Which joins between adjacent modules the generator left open, so the nav graph agrees with
    /// what a player can actually walk through. Mirrors <c>ProceduralCastleGenerator.IsArchwayConnected</c>
    /// and <c>SealOpenArchways</c>: a sealed archway stays closed here too.
    /// </summary>
    public static class CastleNavArchwayRule
    {
        /// <summary>
        /// Room to room: always open, every room has an archway on all four sides. Room to curtain
        /// wall: open only at the gatehouse and at a strip entrance. Along the strip: open. The
        /// gate's outward side: closed, the gate is sealed (<see cref="CastleBoundary"/>).
        /// </summary>
        public static bool IsOpen(ProceduralCastleData data, ProceduralCastleData.PlacedModule a,
            ProceduralCastleData.PlacedModule b)
        {
            bool roomA = ProceduralCastleGenerator.IsEnclosedRoom(a.Zone);
            bool roomB = ProceduralCastleGenerator.IsEnclosedRoom(b.Zone);
            if (roomA && roomB)
                return true;
            if (roomA != roomB)
            {
                ProceduralCastleData.PlacedModule wall = roomA ? b : a;
                return wall.IsExtractionExit || data.EntranceCells.Contains(wall.GridPosition);
            }
            return !IsOutsideGate(a, b) && !IsOutsideGate(b, a);
        }

        // The cell beyond the gatehouse is the drawbridge; the sealed gate keeps it cut off.
        private static bool IsOutsideGate(ProceduralCastleData.PlacedModule gate, ProceduralCastleData.PlacedModule other)
        {
            if (!gate.IsExtractionExit)
                return false;
            Vector3 outward = -CastleSpawnResolver.InwardDirection(gate.GridPosition);
            return other.GridPosition - gate.GridPosition ==
                   new Vector2Int(Mathf.RoundToInt(outward.x), Mathf.RoundToInt(outward.z));
        }
    }
}

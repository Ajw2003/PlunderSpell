using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Castle
{
    /// <summary>One door to spawn: where its archway sill centre is, which way it faces, and which zone's opening height it uses.</summary>
    public readonly struct PlannedDoor
    {
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly CastleZone Zone;

        public PlannedDoor(Vector3 position, Quaternion rotation, CastleZone zone)
        {
            Position = position;
            Rotation = rotation;
            Zone = zone;
        }
    }

    /// <summary>
    /// Lists the doors of a castle from its layout (#248, docs/plans/multi-floor-castle.md section 3): every open
    /// archway between an inner-ward room and a bailey room on the ground floor, the archway out of each stair head
    /// into the keep, and the archway at the crypt stair's foot. Pure, and the same on every peer.
    /// </summary>
    public static class CastleDoorPlanner
    {
        // Module geometry shared with ProceduralCastleGenerator's door plugs (room_kit.py): wall 0.5, floor 0.3, footprint 12.
        private const float ArchwayInset = 12f / 2f - 0.5f / 2f;
        private const float FloorThickness = 0.3f;

        // Fixed order so the list never depends on hashing.
        private static readonly Vector2Int[] Sides =
            { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        public static List<PlannedDoor> Plan(ProceduralCastleData data)
        {
            var doors = new List<PlannedDoor>();
            for (int i = 0; i < data.PlacedModules.Count; i++)
            {
                ProceduralCastleData.PlacedModule module = data.PlacedModules[i];
                if (module.Storeys > 1 && module.ExitLevel != CastleLevels.Ground)
                    AddStairExit(data, module, doors);
                if (module.Zone == CastleZone.InnerWard && module.Level <= CastleLevels.Ground && CastleLevels.Ground <= module.TopLevel)
                    AddWardBaileyDoors(data, module, doors);
            }
            return doors;
        }

        // The head of an up-stair opens into the keep, the foot of the down-stair into the crypt; each opens one way only.
        private static void AddStairExit(ProceduralCastleData data, ProceduralCastleData.PlacedModule stair, List<PlannedDoor> doors)
        {
            CastleZone zone = stair.ExitLevel == CastleLevels.Keep ? CastleZone.Keep : CastleZone.Crypt;
            if (TryFind(data, stair.GridPosition + stair.ExitFacing, stair.ExitLevel, out ProceduralCastleData.PlacedModule next) &&
                CastleNavArchwayRule.IsOpen(data, stair, next, stair.ExitLevel))
                doors.Add(Door(stair, stair.ExitFacing, stair.ExitLevel, zone));
        }

        private static void AddWardBaileyDoors(ProceduralCastleData data, ProceduralCastleData.PlacedModule ward, List<PlannedDoor> doors)
        {
            foreach (Vector2Int side in Sides)
            {
                if (TryFind(data, ward.GridPosition + side, CastleLevels.Ground, out ProceduralCastleData.PlacedModule other) &&
                    other.Zone == CastleZone.OuterBailey &&
                    CastleNavArchwayRule.IsOpen(data, ward, other, CastleLevels.Ground))
                    doors.Add(Door(ward, side, CastleLevels.Ground, CastleZone.InnerWard));
            }
        }

        private static PlannedDoor Door(ProceduralCastleData.PlacedModule module, Vector2Int side, int level, CastleZone zone)
        {
            Vector3 toward = new Vector3(side.x, 0f, side.y);
            Vector3 sill = new Vector3(module.Position.x, CastleLevels.RootY(level) + FloorThickness, module.Position.z) + toward * ArchwayInset;
            return new PlannedDoor(sill, Quaternion.LookRotation(toward), zone);
        }

        // A linear scan: a castle has under 70 modules and this runs once per raid.
        private static bool TryFind(ProceduralCastleData data, Vector2Int cell, int level, out ProceduralCastleData.PlacedModule found)
        {
            foreach (ProceduralCastleData.PlacedModule module in data.PlacedModules)
            {
                if (module.GridPosition == cell && module.Level <= level && level <= module.TopLevel)
                {
                    found = module;
                    return true;
                }
            }
            found = default;
            return false;
        }
    }
}

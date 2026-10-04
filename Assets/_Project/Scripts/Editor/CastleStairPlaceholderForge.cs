using System.Collections.Generic;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Builds the placeholder stairs for the stacked castle (#247): a two-storey box with a ramp, until the period
    /// stairs are modelled (#249). Up: a ward room (archways all round) with a ramp from 0.30 to the keep floor at
    /// 4.60, and a head floor that opens only north. Down: a crypt room that opens only north, with a ramp up to a
    /// ward room at 3.60 (archways all round). Numbers: docs/plans/multi-floor-castle-step1-plan.md, Task 4.
    /// </summary>
    public static class CastleStairPlaceholderForge
    {
        private const string PrefabDir = "Assets/_Project/Prefabs/Castle";
        private static readonly string[] Registries = { "CastleRoomRegistry", "CastleRoomRegistry_BronzeAge", "CastleRoomRegistry_LateMedieval" };
        private const float Half = 6f, Wall = 0.5f, In = Half - Wall, Slab = 0.3f, ArchHalf = 1.3f;

        [MenuItem("Tools/Plunderspell/Forge Stair Placeholders")]
        public static void Forge()
        {
            GameObject up = BuildUp();
            GameObject down = BuildDown();
            foreach (string name in Registries)
            {
                var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>($"Assets/_Project/Data/Castle/{name}.asset");
                Register(registry, ProceduralCastleGenerator.StairUpId, up, 4.6f);
                Register(registry, ProceduralCastleGenerator.StairDownId, down, 3.6f);
                EditorUtility.SetDirty(registry);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[StairForge] Built StairUp and StairDown placeholders and registered them in 3 registries. Bake the nav tiles next.");
        }

        private static GameObject BuildUp()
        {
            var root = new GameObject("StairUpPlaceholder");
            Box(root, "LobbySlab", -Half, 0f, -Half, Half, Slab, Half);
            Walls(root, "Lobby", Slab, Slab + 4.0f, 2.88f, allSides: true);                 // ward room, 4.00 clear
            // Head floor at 4.30-4.60 with a well over the ramp's upper part (x -1.5..4.4, z 1.4..3.8): from x -1.5 a guard on the ramp has 2.40 m under the slab edge (needs 2.30).
            Box(root, "HeadSlabW", -Half, 4.3f, -Half, -1.5f, 4.6f, Half);
            Box(root, "HeadSlabE", 4.4f, 4.3f, -Half, Half, 4.6f, Half);
            Box(root, "HeadSlabS", -1.5f, 4.3f, -Half, 4.4f, 4.6f, 1.4f);
            Box(root, "HeadSlabN", -1.5f, 4.3f, 3.8f, 4.4f, 4.6f, Half);
            Walls(root, "Head", 4.6f, 4.6f + 4.6f, 3.31f, allSides: false);                  // keep storey, opens north only
            Ramp(root, "Ramp", -5.0f, Slab, 4.4f, 4.6f, 1.6f, 3.6f);                         // along +X, z 1.6..3.6
            return Save(root, ProceduralCastleGenerator.StairUpId);
        }

        private static GameObject BuildDown()
        {
            var root = new GameObject("StairDownPlaceholder");
            Box(root, "FootSlab", -Half, 0f, -Half, Half, Slab, Half);
            Walls(root, "Foot", Slab, 3.3f, 2.16f, allSides: false);                        // crypt storey, opens north only
            // Lobby floor at 3.30-3.60 with a well over the ramp (x -2.0..3.9, z -3.8..-1.4): 2.40 m headroom at the slab edge (needs 2.30).
            Box(root, "LobbySlabW", -Half, 3.3f, -Half, -2.0f, 3.6f, Half);
            Box(root, "LobbySlabE", 3.9f, 3.3f, -Half, Half, 3.6f, Half);
            Box(root, "LobbySlabS", -2.0f, 3.3f, -Half, 3.9f, 3.6f, -3.8f);
            Box(root, "LobbySlabN", -2.0f, 3.3f, -1.4f, 3.9f, 3.6f, Half);
            Walls(root, "Lobby", 3.6f, 3.6f + 4.0f, 2.88f, allSides: true);                  // ward room, 4.00 clear
            Ramp(root, "Ramp", -3.3f, Slab, 3.9f, 3.6f, -3.6f, -1.6f);                       // along +X, z -3.6..-1.6
            return Save(root, ProceduralCastleGenerator.StairDownId);
        }

        // Four walls between y0 and y1; each has a centred archway of the given height, except that with
        // allSides false only the north (+Z) wall has one.
        private static void Walls(GameObject root, string name, float y0, float y1, float archHeight, bool allSides)
        {
            for (int side = 0; side < 4; side++)
            {
                bool open = allSides || side == 0;
                // side 0 north (+Z), 1 east (+X), 2 south (-Z), 3 west (-X)
                float sign = side < 2 ? 1f : -1f;
                bool alongX = side % 2 == 0;
                if (!open)
                {
                    WallBox(root, $"{name}Wall{side}", alongX, sign, -Half, Half, y0, y1);
                    continue;
                }
                WallBox(root, $"{name}Wall{side}a", alongX, sign, -Half, -ArchHalf, y0, y1);
                WallBox(root, $"{name}Wall{side}b", alongX, sign, ArchHalf, Half, y0, y1);
                WallBox(root, $"{name}Wall{side}Lintel", alongX, sign, -ArchHalf, ArchHalf, y0 + archHeight, y1);
            }
        }

        private static void WallBox(GameObject root, string name, bool alongX, float sign, float a0, float a1, float y0, float y1)
        {
            float face0 = sign > 0 ? In : -Half, face1 = sign > 0 ? Half : -In;
            if (alongX)
                Box(root, name, a0, y0, face0, a1, y1, face1);
            else
                Box(root, name, face0, y0, a0, face1, y1, a1);
        }

        private static void Box(GameObject root, string name, float x0, float y0, float z0, float x1, float y1, float z1)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, (z0 + z1) / 2f);
            cube.transform.localScale = new Vector3(x1 - x0, y1 - y0, z1 - z0);
            cube.isStatic = true;
        }

        // A 0.3 m thick ramp rising along +X from (xFoot, yFoot) to (xHead, yHead), between z0 and z1.
        private static void Ramp(GameObject root, string name, float xFoot, float yFoot, float xHead, float yHead, float z0, float z1)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(root.transform, false);
            float run = xHead - xFoot, rise = yHead - yFoot, length = Mathf.Sqrt(run * run + rise * rise);
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            cube.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            // The top face runs from foot to head; the box hangs 0.15 m below that line.
            Vector3 mid = new Vector3((xFoot + xHead) / 2f, (yFoot + yHead) / 2f, (z0 + z1) / 2f);
            cube.transform.localPosition = mid - cube.transform.localRotation * new Vector3(0f, 0.15f, 0f);
            cube.transform.localScale = new Vector3(length, 0.3f, z1 - z0);
            cube.isStatic = true;
        }

        private static GameObject Save(GameObject root, string roomId)
        {
            var module = root.AddComponent<CastleRoomModule>();
            module.RoomId = roomId;
            module.Zone = CastleZone.InnerWard;
            string path = $"{PrefabDir}/{root.name}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void Register(CastleRoomRegistry registry, string roomId, GameObject prefab, float upperFloor)
        {
            registry.Stairs.RemoveAll(s => s != null && s.RoomId == roomId);
            registry.Stairs.Add(new CastleRoomModuleData
            {
                RoomId = roomId, Zone = CastleZone.InnerWard, Prefab = prefab, Weight = 1, UpperFloorHeight = upperFloor,
            });
        }
    }
}

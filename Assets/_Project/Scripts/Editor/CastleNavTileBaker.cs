using System.Collections.Generic;
using System.IO;
using System.Text;
using Plunderspell.Castle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Probes every room module prefab once, with physics casts against its colliders, and writes a
    /// <see cref="CastleNavTile"/> into its registry entry. Re-run after the castle meshes change.
    /// Each module is placed at the origin in a throwaway preview scene, so no open scene is touched.
    /// </summary>
    public static class CastleNavTileBaker
    {
        private const string OutputDir = "docs/generated/nav-tiles-2026-10-02";
        private const string DefaultRegistryName = "CastleRoomRegistry";

        private const float GuardRadius = 0.4f;
        private const float GuardHeight = 1.85f;
        private const float RayTop = 14f;
        private const float RayBottom = -1f;
        private const float MinNormalY = 0.7f;
        private const float ArchHalfWidth = 1.3f;
        private const float MaxDoorwayFloor = 1.0f;
        private const int N = CastleNavTile.Size;
        // Scratch surfaces per column while scanning: wall tops and lintels count until the flood fill drops them.
        private const int Scratch = 6;
        // Curtain-wall pieces carry no floor of their own: the strip they stand on is the scene's ground plane.
        private const float StripGroundHeight = 0f;
        private const string DrawbridgeId = "Drawbridge";

        /// <summary>Menu entry: bakes and logs the summary.</summary>
        [MenuItem("Tools/Plunderspell/Bake Castle Nav Tiles")]
        public static void BakeMenu() => Debug.Log(Bake());

        /// <summary>
        /// Bakes every module of every room registry, writes overlays and a summary, and returns the
        /// summary. Every Age has its own registry with its own entries (a shared piece such as WallCorner
        /// is a separate entry in each), so all of them are baked: an unbaked registry gives that Age's
        /// castle an empty walk map, and its guards cannot move at all.
        /// </summary>
        public static string Bake()
        {
            string[] registryGuids = AssetDatabase.FindAssets("t:" + nameof(CastleRoomRegistry));
            if (registryGuids.Length == 0)
                return "[NavTiles] No CastleRoomRegistry assets found.";
            Directory.CreateDirectory(OutputDir);

            var report = new StringBuilder();
            report.AppendLine("Castle nav tiles, 24x24 cells of 0.5 m, 2 layers. Guard capsule r=0.4 h=1.85.");
            report.AppendLine("module | walkable% (columns with any walkable surface, of 576; total surfaces) | levels | portals N/E/S/W | dropped unreachable | bytes");

            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                foreach (string guid in registryGuids)
                {
                    var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(AssetDatabase.GUIDToAssetPath(guid));
                    report.AppendLine($"## {registry.name}");
                    BakeRegistry(registry, OverlayFolderFor(registry), scene, report);
                    EditorUtility.SetDirty(registry);
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }

            AssetDatabase.SaveAssets();
            File.WriteAllText(Path.Combine(OutputDir, "summary.txt"), report.ToString());
            return report.ToString();
        }

        // The default registry keeps the folder root (the overlays #220 was checked against); every other
        // Age gets a folder of its own, because a shared piece such as WallCorner is a different model there.
        private static string OverlayFolderFor(CastleRoomRegistry registry)
        {
            string folder = registry.name == DefaultRegistryName ? OutputDir : Path.Combine(OutputDir, registry.name);
            Directory.CreateDirectory(folder);
            return folder;
        }

        private static void BakeRegistry(CastleRoomRegistry registry, string overlayFolder, Scene scene, StringBuilder report)
        {
            foreach (CastleRoomModuleData entry in System.Linq.Enumerable.Concat(registry.Modules, registry.Stairs))
            {
                if (entry == null || entry.Prefab == null)
                    continue;
                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab);
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetPositionAndRotation(Vector3.zero, entry.Prefab.transform.rotation); // the FBX import rotation, as the generator keeps it
                Physics.SyncTransforms();
                // The drawbridge spans a moat, so a virtual ground under it would be a lie.
                bool strip = entry.Zone == CastleZone.CurtainWall && entry.RoomId != DrawbridgeId;
                CastleNavTile tile = BakeModule(scene.GetPhysicsScene(), strip, entry.UpperFloorHeight, out int dropped);
                Object.DestroyImmediate(go);

                entry.NavTile = tile;
                WriteOverlay(overlayFolder, entry.RoomId, tile);
                report.AppendLine(SummaryLine(entry.RoomId, tile, dropped));
            }
        }

        private static string SummaryLine(string roomId, CastleNavTile tile, int dropped)
        {
            int walk = 0, columns = 0;
            for (int i = 0; i < N * N; i++)
            {
                walk += tile.Walkable[i] + tile.Walkable[N * N + i];
                if (tile.Walkable[i] + tile.Walkable[N * N + i] > 0)
                    columns++;
            }
            int bytes = tile.Walkable.Length + tile.HeightCm.Length * 2 +
                        (tile.PortalNorth.Length + tile.PortalEast.Length +
                         tile.PortalSouth.Length + tile.PortalWest.Length) * 2;
            return $"{roomId} | {100f * columns / (N * N):F1} ({walk} surfaces) | {tile.LevelCount} | " +
                   $"{tile.PortalNorth.Length}/{tile.PortalEast.Length}/{tile.PortalSouth.Length}/{tile.PortalWest.Length}" +
                   $" | {dropped} | {bytes}";
        }

        private static CastleNavTile BakeModule(PhysicsScene physics, bool strip, float upperFloor, out int dropped)
        {
            int cells = N * N;
            var walk = new bool[Scratch * cells];
            var height = new float[Scratch * cells];
            var capsuleHits = new Collider[1];
            float lift = CastleNavTile.StepHeight + 0.01f;

            for (int z = 0; z < N; z++)
            {
                for (int x = 0; x < N; x++)
                {
                    float wx = (x + 0.5f) * CastleNavTile.CellSize - N * CastleNavTile.CellSize * 0.5f;
                    float wz = (z + 0.5f) * CastleNavTile.CellSize - N * CastleNavTile.CellSize * 0.5f;
                    float y = RayTop;
                    int layer = 0;
                    while (layer < Scratch && y > RayBottom &&
                           physics.Raycast(new Vector3(wx, y, wz), Vector3.down, out RaycastHit hit,
                               y - RayBottom, ~0, QueryTriggerInteraction.Ignore))
                    {
                        y = hit.point.y - 0.02f;
                        if (hit.normal.y < MinNormalY)
                            continue;
                        float h = hit.point.y;
                        Vector3 bottom = new Vector3(wx, h + lift + GuardRadius, wz);
                        Vector3 top = new Vector3(wx, h + GuardHeight - GuardRadius, wz);
                        int n = physics.OverlapCapsule(bottom, top, GuardRadius, capsuleHits, ~0,
                            QueryTriggerInteraction.Ignore);
                        if (n > 0)
                            continue;
                        int idx = (layer * N + z) * N + x;
                        walk[idx] = true;
                        height[idx] = h;
                        layer++;
                    }

                    // Curtain pieces have no floor mesh, only walls and towers; the ground between
                    // them is walkable where the capsule clears. Added last, so it stays the lowest.
                    if (strip && layer < Scratch && !HasDoorwayFloor(walk, height, x, z))
                    {
                        Vector3 gBottom = new Vector3(wx, StripGroundHeight + lift + GuardRadius, wz);
                        Vector3 gTop = new Vector3(wx, StripGroundHeight + GuardHeight - GuardRadius, wz);
                        if (physics.OverlapCapsule(gBottom, gTop, GuardRadius, capsuleHits, ~0,
                                QueryTriggerInteraction.Ignore) == 0)
                        {
                            int gi = (layer * N + z) * N + x;
                            walk[gi] = true;
                            height[gi] = StripGroundHeight;
                        }
                    }
                }
            }

            // Portals: the edge cells inside each archway opening. Seeded from the column's lowest
            // surface, because the lintel over the opening is also a walkable-looking surface.
            var north = new List<ushort>();
            var east = new List<ushort>();
            var south = new List<ushort>();
            var west = new List<ushort>();
            for (int i = 0; i < N; i++)
            {
                float c = (i + 0.5f) * CastleNavTile.CellSize - N * CastleNavTile.CellSize * 0.5f;
                // A strip piece opens along its whole edge: the yard runs on into the next piece, and
                // an entrance is wherever the room's archway meets it.
                if (!strip && Mathf.Abs(c) > ArchHalfWidth)
                    continue;
                if (LowestSurface(walk, height, i, N - 1) >= 0 || SurfaceAt(walk, height, i, N - 1, upperFloor) >= 0) north.Add((ushort)((N - 1) * N + i));
                if (LowestSurface(walk, height, N - 1, i) >= 0 || SurfaceAt(walk, height, N - 1, i, upperFloor) >= 0) east.Add((ushort)(i * N + N - 1));
                if (LowestSurface(walk, height, i, 0) >= 0 || SurfaceAt(walk, height, i, 0, upperFloor) >= 0) south.Add((ushort)i);
                if (LowestSurface(walk, height, 0, i) >= 0 || SurfaceAt(walk, height, 0, i, upperFloor) >= 0) west.Add((ushort)(i * N));
            }

            // Wall tops, lintels and sealed-off pockets are walkable on their own; keep only what
            // connects to an archway, stepping between cells no higher than the step height.
            var reached = new bool[walk.Length];
            var stack = new Stack<int>();
            var sides = new[] { north, east, south, west };
            foreach (List<ushort> side in sides)
            {
                foreach (ushort p in side)
                {
                    foreach (int s in new[] { LowestSurface(walk, height, p % N, p / N), SurfaceAt(walk, height, p % N, p / N, upperFloor) })
                    {
                        if (s >= 0 && !reached[s])
                        {
                            reached[s] = true;
                            stack.Push(s);
                        }
                    }
                }
            }
            int[] dx = { 1, -1, 0, 0 };
            int[] dz = { 0, 0, 1, -1 };
            while (stack.Count > 0)
            {
                int cur = stack.Pop();
                int cz = (cur % cells) / N, cx = cur % N;
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d], nz = cz + dz[d];
                    if (nx < 0 || nz < 0 || nx >= N || nz >= N)
                        continue;
                    for (int nl = 0; nl < Scratch; nl++)
                    {
                        int ni = (nl * N + nz) * N + nx;
                        if (!walk[ni] || reached[ni])
                            continue;
                        if (Mathf.Abs(height[ni] - height[cur]) > CastleNavTile.StepHeight)
                            continue;
                        reached[ni] = true;
                        stack.Push(ni);
                    }
                }
            }

            var tile = new CastleNavTile
            {
                Walkable = new byte[CastleNavTile.Layers * cells],
                HeightCm = new short[CastleNavTile.Layers * cells],
                PortalNorth = north.ToArray(),
                PortalEast = east.ToArray(),
                PortalSouth = south.ToArray(),
                PortalWest = west.ToArray(),
            };
            dropped = 0;
            var used = new bool[CastleNavTile.Layers];
            for (int z = 0; z < N; z++)
            {
                for (int x = 0; x < N; x++)
                {
                    int layer = 0;
                    // Scratch surfaces are in descending height; pack the reached ones lowest first.
                    for (int sl = Scratch - 1; sl >= 0; sl--)
                    {
                        int si = (sl * N + z) * N + x;
                        if (!walk[si])
                            continue;
                        if (!reached[si] || layer >= CastleNavTile.Layers)
                        {
                            dropped++;
                            continue;
                        }
                        int ti = CastleNavTile.Index(layer, x, z);
                        tile.Walkable[ti] = 1;
                        tile.HeightCm[ti] = (short)Mathf.RoundToInt(height[si] * CastleNavTile.HeightScale);
                        used[layer] = true;
                        layer++;
                    }
                }
            }
            for (int l = 0; l < used.Length; l++)
            {
                if (used[l])
                    tile.LevelCount++;
            }
            return tile;
        }

        private static bool HasDoorwayFloor(bool[] walk, float[] height, int x, int z)
        {
            return LowestSurface(walk, height, x, z) >= 0;
        }

        /// <summary>Scratch index of the lowest walkable surface in a column when it is at doorway floor
        /// level (a lintel or wall top is not a doorway), or -1. Jamb cells whose floor is blocked by
        /// the capsule give -1, so a portal is the archway's clear width, not its full 2.6 m.</summary>
        private static int LowestSurface(bool[] walk, float[] height, int x, int z)
        {
            int best = -1;
            for (int l = 0; l < Scratch; l++)
            {
                int i = (l * N + z) * N + x;
                if (walk[i] && (best < 0 || height[i] < height[best]))
                    best = i;
            }
            return best >= 0 && height[best] <= MaxDoorwayFloor ? best : -1;
        }

        // The surface in a column whose height is within 0.1 m of a floor, or -1: a stair's upper floor
        // (#247) is an archway floor too, though it is not the column's lowest surface.
        private static int SurfaceAt(bool[] walk, float[] height, int x, int z, float floor)
        {
            if (floor <= 0f)
                return -1;
            for (int layer = 0; layer < Scratch; layer++)
            {
                int i = (layer * N + z) * N + x;
                if (walk[i] && Mathf.Abs(height[i] - floor) < 0.1f)
                    return i;
            }
            return -1;
        }

        /// <summary>Top-down overlay, north up: one panel per layer. Green walkable (brighter =
        /// higher), red blocked or empty, blue portal.</summary>
        private static void WriteOverlay(string folder, string roomId, CastleNavTile tile)
        {
            const int px = 16;
            int w = CastleNavTile.Layers * N * px + (CastleNavTile.Layers - 1) * 8;
            int h = N * px;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(30, 30, 30, 255);

            float maxH = 0.5f;
            for (int i = 0; i < tile.HeightCm.Length; i++)
            {
                if (tile.Walkable[i] != 0)
                    maxH = Mathf.Max(maxH, tile.HeightCm[i] / CastleNavTile.HeightScale);
            }

            for (int layer = 0; layer < CastleNavTile.Layers; layer++)
            {
                for (int z = 0; z < N; z++)
                {
                    for (int x = 0; x < N; x++)
                    {
                        Color32 col;
                        if (tile.IsWalkable(layer, x, z))
                        {
                            float t = Mathf.Clamp01(tile.Height(layer, x, z) / maxH);
                            col = new Color32(20, (byte)(90 + 165 * t), 20, 255);
                        }
                        else
                        {
                            col = layer == 0 ? new Color32(200, 40, 40, 255) : new Color32(60, 30, 30, 255);
                        }
                        if (layer == 0 && IsPortal(tile, x, z))
                            col = new Color32(40, 120, 255, 255);
                        int ox = layer * (N * px + 8) + x * px;
                        int oy = z * px; // texture row 0 is the bottom, so +Z (north) is up
                        for (int py = 0; py < px; py++)
                        {
                            for (int pxx = 0; pxx < px; pxx++)
                            {
                                bool edge = py == 0 || pxx == 0;
                                pixels[(oy + py) * w + ox + pxx] = edge
                                    ? new Color32((byte)(col.r / 2), (byte)(col.g / 2), (byte)(col.b / 2), 255)
                                    : col;
                            }
                        }
                    }
                }
            }
            tex.SetPixels32(pixels);
            File.WriteAllBytes(Path.Combine(folder, roomId + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        private static bool IsPortal(CastleNavTile tile, int x, int z)
        {
            ushort cell = (ushort)(z * N + x);
            return Contains(tile.PortalNorth, cell) || Contains(tile.PortalEast, cell) ||
                   Contains(tile.PortalSouth, cell) || Contains(tile.PortalWest, cell);
        }

        private static bool Contains(ushort[] a, ushort v)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == v)
                    return true;
            }
            return false;
        }
    }
}

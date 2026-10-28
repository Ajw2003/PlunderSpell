using System.Collections.Generic;
using System.IO;
using System.Linq;
using Plunderspell.Castle;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Splits each castle room mesh into Wall / Roof / Floor / Props parts (#471) so a fracture system can break them
    /// separately. The Blender rooms are boxes, each its own disconnected vertex island, so islands are classified by
    /// their bounds. The root renderer and collider are disabled, not deleted; the parts are Part_* children.
    /// </summary>
    public static class RoomPartSplitter
    {
        public const int Wall = 0, Roof = 1, Floor = 2, Props = 3;
        public static readonly string[] PartNames = { "Wall", "Roof", "Floor", "Props" };

        private const string PrefabRoot = "Assets/_Project/Prefabs/Castle";
        private const string PartDir = "Assets/_Project/Art/Models/Castle/Parts";
        private const string SheetDir = "docs/generated/room-parts-471";
        private const float PerimeterBand = 0.75f, WallMinHeight = 1.5f, SlabMaxThick = 0.6f, SlabMinCover = 0.4f, RoofMinTop = 2f;

        /// <summary>Part (0 Wall, 1 Roof, 2 Floor, 3 Props) of every triangle. Y is up; callers rotate Z-up meshes first.</summary>
        public static int[] Classify(Vector3[] vertices, int[] triangles)
        {
            int triCount = triangles.Length / 3;
            // Union-find over welded positions (1 mm): triangles sharing a position are one island.
            var parent = new int[triCount];
            for (int i = 0; i < triCount; i++) parent[i] = i;
            int Find(int a) { while (parent[a] != a) a = parent[a] = parent[parent[a]]; return a; }
            var owner = new Dictionary<Vector3Int, int>();
            for (int t = 0; t < triCount; t++)
                for (int k = 0; k < 3; k++)
                {
                    Vector3 v = vertices[triangles[t * 3 + k]];
                    var key = new Vector3Int(Mathf.RoundToInt(v.x * 1000f), Mathf.RoundToInt(v.y * 1000f), Mathf.RoundToInt(v.z * 1000f));
                    if (owner.TryGetValue(key, out int other)) parent[Find(t)] = Find(other);
                    else owner[key] = t;
                }

            var islandBounds = new Dictionary<int, Bounds>();
            Bounds mesh = default;
            bool first = true;
            for (int t = 0; t < triCount; t++)
            {
                int root = Find(t);
                for (int k = 0; k < 3; k++)
                {
                    Vector3 v = vertices[triangles[t * 3 + k]];
                    if (first) { mesh = new Bounds(v, Vector3.zero); first = false; } else mesh.Encapsulate(v);
                    if (islandBounds.TryGetValue(root, out Bounds b)) { b.Encapsulate(v); islandBounds[root] = b; }
                    else islandBounds[root] = new Bounds(v, Vector3.zero);
                }
            }

            // Walls stand on the floor slab's edges, so welding merges floor, walls and roof into one shell island: inside a
            // shell each triangle is sorted on its own, and only whole islands clear of the cell's edge are props (#471).
            var normal = new Vector3[triCount];
            var centroid = new Vector3[triCount];
            var area = new float[triCount];
            for (int t = 0; t < triCount; t++)
            {
                Vector3 a = vertices[triangles[t * 3]], b = vertices[triangles[t * 3 + 1]], c = vertices[triangles[t * 3 + 2]];
                Vector3 cross = Vector3.Cross(b - a, c - a);
                area[t] = cross.magnitude * 0.5f;
                normal[t] = cross.sqrMagnitude > 0f ? cross.normalized : Vector3.up;
                centroid[t] = (a + b + c) / 3f;
            }
            // A slab is a band of heights whose flat faces cover over 40% of the footprint: a floor, or the roof if highest.
            var levelArea = new SortedDictionary<int, float>();
            for (int t = 0; t < triCount; t++)
                if (Mathf.Abs(normal[t].y) > 0.7f)
                {
                    int level = Mathf.RoundToInt(centroid[t].y / 0.05f);
                    levelArea[level] = (levelArea.TryGetValue(level, out float sum) ? sum : 0f) + area[t];
                }
            var slabs = new List<Vector2>();   // (bottom, top) of each slab band
            foreach (var kv in levelArea)
            {
                if (kv.Value < SlabMinCover * mesh.size.x * mesh.size.z) continue;
                float y = kv.Key * 0.05f;
                if (slabs.Count > 0 && y - slabs[slabs.Count - 1].y <= SlabMaxThick) slabs[slabs.Count - 1] = new Vector2(slabs[slabs.Count - 1].x, y);
                else slabs.Add(new Vector2(y, y));
            }
            int roofSlab = slabs.Count > 0 && slabs[slabs.Count - 1].y - mesh.min.y > RoofMinTop ? slabs.Count - 1 : -1;
            int SlabAt(float yMin, float yMax)
            {
                for (int i = 0; i < slabs.Count; i++)
                    if (yMin >= slabs[i].x - 0.06f && yMax <= slabs[i].y + 0.06f) return i;
                return -1;
            }

            var result = new int[triCount];
            for (int t = 0; t < triCount; t++)
            {
                Bounds island = islandBounds[Find(t)];
                bool edge = island.min.x <= mesh.min.x + PerimeterBand || island.max.x >= mesh.max.x - PerimeterBand
                         || island.min.z <= mesh.min.z + PerimeterBand || island.max.z >= mesh.max.z - PerimeterBand;
                if (!edge) { result[t] = Props; continue; }
                Vector3 a = vertices[triangles[t * 3]], b = vertices[triangles[t * 3 + 1]], c = vertices[triangles[t * 3 + 2]];
                int slab = SlabAt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)), Mathf.Max(a.y, Mathf.Max(b.y, c.y)));
                float ny = Mathf.Abs(normal[t].y);
                float height = centroid[t].y - mesh.min.y;
                // A slab owns its edges, its top facing up and its bottom facing down; a wall's foot resting on the floor
                // faces down at the floor's top, so it stays with the wall.
                bool slabFace = slab >= 0 && (ny <= 0.7f
                    || (normal[t].y > 0f && Mathf.Abs(centroid[t].y - slabs[slab].y) < 0.06f)
                    || (normal[t].y < 0f && Mathf.Abs(centroid[t].y - slabs[slab].x) < 0.06f));
                if (slabFace) result[t] = slab == roofSlab ? Roof : Floor;
                // Pitched faces high up are roof; the rest of the shell is wall (wall tops and merlons included).
                else if (ny > 0.3f && ny <= 0.7f && height > RoofMinTop) result[t] = Roof;
                else result[t] = Wall;
            }
            return result;
        }

        [MenuItem("Tools/Plunderspell/Castle/Split Castle Rooms Into Parts")]
        public static void SplitMenu()
        {
            Directory.CreateDirectory(PartDir);
            foreach (string path in PartPrefabPaths())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var filter = root.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
                    {
                        Debug.LogWarning($"[RoomParts] {path}: no readable root mesh, skipped.");
                        continue;
                    }
                    foreach (Transform old in root.transform.Cast<Transform>().Where(c => c.name.StartsWith("Part_")).ToArray())
                        Object.DestroyImmediate(old.gameObject);

                    Mesh source = filter.sharedMesh;
                    Material[] mats = root.GetComponent<MeshRenderer>().sharedMaterials;
                    // Classify in upright metres: the Blender rooms carry a Z-up correction and a 100x scale on the root.
                    Matrix4x4 toRoom = Matrix4x4.TRS(Vector3.zero, root.transform.rotation, root.transform.lossyScale);
                    Vector3[] verts = source.vertices;
                    int[] tris = source.triangles;
                    // A door plug is one solid block: every triangle is Wall (0), no classification (#474).
                    int[] part = root.GetComponent<CastleRoomModule>() == null
                        ? new int[tris.Length / 3]
                        : Classify(verts.Select(v => toRoom.MultiplyPoint3x4(v)).ToArray(), tris);

                    string counts = "";
                    for (int p = 0; p < 4; p++)
                    {
                        Mesh partMesh = BuildPart(source, p, part, mats, out Material[] partMats);
                        counts += $" {PartNames[p]}={part.Count(x => x == p)}";
                        if (partMesh == null) continue;
                        partMesh.name = $"{root.name}_{PartNames[p]}";
                        partMesh = SaveMesh(partMesh, $"{PartDir}/{partMesh.name}.asset");

                        var child = new GameObject("Part_" + PartNames[p]);
                        child.transform.SetParent(root.transform, false);
                        child.AddComponent<MeshFilter>().sharedMesh = partMesh;
                        child.AddComponent<MeshRenderer>().sharedMaterials = partMats;
                        child.AddComponent<MeshCollider>().sharedMesh = partMesh;
                    }
                    root.GetComponent<MeshRenderer>().enabled = false;
                    var rootCol = root.GetComponent<MeshCollider>();
                    if (rootCol != null) rootCol.enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[RoomParts] {root.name}: triangles{counts}");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Tools/Plunderspell/Castle/Render Castle Room Part Sheet")]
        public static void SheetMenu()
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SheetDir));
            Directory.CreateDirectory(dir);
            Color[] tint = { Color.grey, Color.red, Color.green, Color.yellow };
            foreach (string path in RoomPrefabPaths())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                var rt = RenderTexture.GetTemporary(768, 768, 24);
                var camGo = new GameObject("PartSheetCam");
                var lightGo = new GameObject("PartSheetLight");
                var made = new List<Material>();
                try
                {
                    // Prefab contents live in their own preview scene, so nothing touches the open scene.
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camGo, root.scene);
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightGo, root.scene);
                    lightGo.AddComponent<Light>().type = LightType.Directional;
                    lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                    Bounds all = default;
                    bool any = false;
                    for (int p = 0; p < 4; p++)
                    {
                        Transform t = root.transform.Find("Part_" + PartNames[p]);
                        if (t == null) continue;
                        var r = t.GetComponent<MeshRenderer>();
                        // Unlit: the lit tint drew one part's faces in another's colour in the prefab preview scene (seen on MedievalStairUp).
                        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                        m.SetColor("_BaseColor", tint[p]);
                        made.Add(m);
                        r.sharedMaterials = Enumerable.Repeat(m, r.sharedMaterials.Length).ToArray();
                        if (any) all.Encapsulate(r.bounds); else { all = r.bounds; any = true; }
                    }
                    if (!any) { Debug.LogWarning($"[RoomParts] {root.name}: no Part_* children, run the split first."); continue; }
                    root.GetComponent<MeshRenderer>().enabled = false;

                    var cam = camGo.AddComponent<Camera>();
                    cam.scene = root.scene;
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
                    cam.targetTexture = rt;
                    cam.fieldOfView = 35f;
                    camGo.transform.position = all.center + new Vector3(1f, 1.1f, -1f).normalized * all.size.magnitude * 1.6f;
                    camGo.transform.LookAt(all.center);
                    cam.Render();

                    RenderTexture.active = rt;
                    var tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(dir, root.name + ".png"), tex.EncodeToPNG());
                    Object.DestroyImmediate(tex);
                    Debug.Log($"[RoomParts] sheet {SheetDir}/{root.name}.png");
                }
                finally
                {
                    RenderTexture.active = null;
                    RenderTexture.ReleaseTemporary(rt);
                    foreach (Material m in made) Object.DestroyImmediate(m);
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        internal static IEnumerable<string> RoomPrefabPaths() =>
            AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("/Dressing/") && AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<CastleRoomModule>() != null)
                .OrderBy(p => p);

        /// <summary>Rooms plus the door plugs of every CastleRoomRegistry asset (the plug prefabs carry no CastleRoomModule, so
        /// they are found through the registries' DoorPlugs lists, not the prefab folder).</summary>
        internal static IEnumerable<string> PartPrefabPaths() =>
            RoomPrefabPaths().Concat(AssetDatabase.FindAssets("t:CastleRoomRegistry")
                .Select(g => AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(AssetDatabase.GUIDToAssetPath(g)))
                .SelectMany(r => r.DoorPlugs)
                .Where(d => d != null && d.Prefab != null && d.Prefab.GetComponent<CastleRoomModule>() == null)
                .Select(d => AssetDatabase.GetAssetPath(d.Prefab))
                .Distinct().OrderBy(p => p));

        /// <summary>The triangles of one part as a new mesh with the used submeshes only; null if the part is empty.</summary>
        private static Mesh BuildPart(Mesh src, int p, int[] part, Material[] mats, out Material[] partMats)
        {
            var usedMats = new List<Material>();
            var newVerts = new List<int>();
            var remap = new Dictionary<int, int>();
            var subTris = new List<List<int>>();
            int[] all = src.triangles;
            for (int s = 0; s < src.subMeshCount; s++)
            {
                var list = new List<int>();
                var desc = src.GetSubMesh(s);
                int[] idx = src.GetTriangles(s);
                // Submesh triangles follow the order of Mesh.triangles, so the global triangle number is start/3 + i.
                for (int i = 0; i < idx.Length; i += 3)
                {
                    if (part[desc.indexStart / 3 + i / 3] != p) continue;
                    for (int k = 0; k < 3; k++)
                    {
                        if (!remap.TryGetValue(idx[i + k], out int n)) { n = newVerts.Count; remap[idx[i + k]] = n; newVerts.Add(idx[i + k]); }
                        list.Add(n);
                    }
                }
                if (list.Count == 0) continue;
                subTris.Add(list);
                usedMats.Add(mats[Mathf.Min(s, mats.Length - 1)]);
            }
            partMats = usedMats.ToArray();
            if (subTris.Count == 0) return null;

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(newVerts.Select(i => src.vertices[i]).ToList());
            if (src.normals.Length == src.vertexCount) mesh.SetNormals(newVerts.Select(i => src.normals[i]).ToList());
            if (src.tangents.Length == src.vertexCount) mesh.SetTangents(newVerts.Select(i => src.tangents[i]).ToList());
            if (src.uv.Length == src.vertexCount) mesh.SetUVs(0, newVerts.Select(i => src.uv[i]).ToList());
            if (src.uv2.Length == src.vertexCount) mesh.SetUVs(1, newVerts.Select(i => src.uv2[i]).ToList());
            if (src.colors.Length == src.vertexCount) mesh.SetColors(newVerts.Select(i => src.colors[i]).ToList());
            mesh.subMeshCount = subTris.Count;
            for (int s = 0; s < subTris.Count; s++) mesh.SetTriangles(subTris[s], s);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Writes the mesh to the path as a fresh asset. Copying onto the old asset left the GPU drawing the old
        /// geometry (seen on MedievalStairUp's roof); every run reassigns the parts, so a new GUID costs nothing.</summary>
        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
    }
}

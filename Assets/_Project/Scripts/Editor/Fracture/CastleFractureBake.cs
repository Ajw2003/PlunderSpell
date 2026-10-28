using System.Collections.Generic;
using Plunderspell.Fracture;
using Plunderspell.Fracture.Editor;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Bakes the fracture data for each castle room part (#472, #490): clips the split part meshes (#471) to a 1.5 m grid so
    /// a hit breaks off whole 1.5 m clusters, gives each cluster its stone edge, makes the mesh double-sided, saves a new
    /// mesh asset, and bakes the FracturableObject graph. Always starts from the split output, so it can be rerun.
    /// </summary>
    public static class CastleFractureBake
    {
        private const string PartDir = "Assets/_Project/Art/Models/Castle/Parts";
        private const string FractureDir = PartDir + "/Fracture";
        // 1.5 m clusters for the Steam Deck budget (#475).
        private const float CellMetres = 1.5f;
        private const float WeldMetres = 0.01f;

        [MenuItem("Tools/Plunderspell/Castle/Bake Castle Room Fracture")]
        public static void BakeMenu()
        {
            if (!AssetDatabase.IsValidFolder(FractureDir)) AssetDatabase.CreateFolder(PartDir, "Fracture");
            long totalClusters = 0, surfaceTriangles = 0, twinTriangles = 0, edgeTriangles = 0, propTriangles = 0;
            foreach (string path in RoomPartSplitter.PartPrefabPaths())
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string log = "";
                    foreach (string name in RoomPartSplitter.PartNames)
                    {
                        Transform part = root.transform.Find("Part_" + name);
                        // The split output, never the current mesh of the part, which may be a previous bake.
                        Mesh source = AssetDatabase.LoadAssetAtPath<Mesh>($"{PartDir}/{root.name}_{name}.asset");
                        if (part == null || source == null) continue;

                        // Props are not clipped (#475): each connected island is one cluster.
                        // The stone edge is half the part's thickness.
                        float thickness = name == "Wall" ? 0.5f : name == "Floor" || name == "Roof" ? 0.3f : 0f;
                        Matrix4x4 toWorld = part.localToWorldMatrix;
                        Mesh surface;
                        int[] clusters;
                        surface = name == "Props" ? ClusterIslands(source, toWorld, out clusters) : ClipToGrid(source, toWorld, out clusters);
                        // Twins follow their originals inside each submesh and keep the cluster; kind 0 front, 1 back twin.
                        var doubled = new List<int>(clusters.Length * 2);
                        var kindList = new List<byte>(clusters.Length * 2);
                        int at = 0;
                        for (int s = 0; s < surface.subMeshCount; s++)
                        {
                            int count = surface.GetTriangles(s).Length / 3;
                            doubled.AddRange(new System.ArraySegment<int>(clusters, at, count));
                            doubled.AddRange(new System.ArraySegment<int>(clusters, at, count));
                            for (int k = 0; k < count; k++) kindList.Add(0);
                            for (int k = 0; k < count; k++) kindList.Add(1);
                            at += count;
                        }
                        Mesh mesh = DoubleSidedMeshGenerator.CreateDoubleSidedMesh(surface);
                        Object.DestroyImmediate(surface);
                        clusters = doubled.ToArray();
                        byte[] kinds = kindList.ToArray();
                        if (thickness > 0f)
                        {
                            // After double-siding: the edge quads are single-sided, only the hole side is ever seen.
                            Mesh edged = AddStoneEdges(mesh, toWorld, ref clusters, ref kinds, thickness * 0.5f / part.lossyScale.x);
                            Object.DestroyImmediate(mesh);
                            mesh = edged;
                        }
                        mesh.name = $"{root.name}_{name}_Fracture";
                        string meshPath = $"{FractureDir}/{mesh.name}.asset";
                        AssetDatabase.DeleteAsset(meshPath); // a fresh asset every time, never overwritten in place
                        AssetDatabase.CreateAsset(mesh, meshPath);

                        part.GetComponent<MeshFilter>().sharedMesh = mesh;
                        // The collider keeps the one-sided split mesh (same shape): a double-sided one stopped guards on stairs.
                        part.GetComponent<MeshCollider>().sharedMesh = source;
                        if (!part.TryGetComponent(out FracturableObject fo)) fo = part.gameObject.AddComponent<FracturableObject>();
                        fo.fractureMesh = mesh;
                        fo.destroyWhenEmpty = false;
                        fo.preInitialize = false;
                        // The castle's own shader keeps drawing what is left; the fracture shader draws only the debris.
                        fo.keepMeshRenderer = true;
                        // The stone edges are baked into the mesh now, so no run-time caps (#477).
                        fo.capDepthMetres = 0f;
                        fo.despawnTime = 2f;
                        // mergeDistance is in the mesh's own space; the rooms are stored at 1/100 size, so the 0.05 default
                        // welded everything within 5 m and the graph build ran out of memory. 1 cm in metres instead.
                        fo.mergeDistance = WeldMetres / part.lossyScale.x;
                        fo.BakeFractureGraph(clusters, kinds);
                        int clusterCount = 0;
                        foreach (int c in clusters) clusterCount = Mathf.Max(clusterCount, c + 1);
                        foreach (byte k in kinds)
                        {
                            if (name == "Props") propTriangles++;
                            else if (k == 0) surfaceTriangles++;
                            else if (k == 1) twinTriangles++;
                            else edgeTriangles++;
                        }
                        totalClusters += clusterCount;
                        log += $" {name}={source.triangles.Length / 3}->{mesh.triangles.Length / 3} ({clusterCount} clusters)";
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[CastleFracture] {root.name}: triangles before->after{log}");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[CastleFracture] total triangles {surfaceTriangles + twinTriangles + edgeTriangles + propTriangles}, clusters {totalClusters}: surface {surfaceTriangles}, back twin {twinTriangles}, stone edge {edgeTriangles}, props {propTriangles}");
        }

        private struct Channels
        {
            public List<Vector3> Verts, Normals;
            public List<Vector2> Uv, Uv2;
            public List<Vector4> Tangents;
            public List<Color> Colors;
            public bool HasN, HasUv, HasUv2, HasT, HasC;
            public List<List<int>> Subs;

            public static Channels Read(Mesh src)
            {
                var c = new Channels { Verts = new List<Vector3>(), Normals = new List<Vector3>(), Uv = new List<Vector2>(), Uv2 = new List<Vector2>(), Tangents = new List<Vector4>(), Colors = new List<Color>(), Subs = new List<List<int>>() };
                src.GetVertices(c.Verts); src.GetNormals(c.Normals); src.GetUVs(0, c.Uv); src.GetUVs(1, c.Uv2); src.GetTangents(c.Tangents); src.GetColors(c.Colors);
                int n = c.Verts.Count;
                c.HasN = c.Normals.Count == n; c.HasUv = c.Uv.Count == n; c.HasUv2 = c.Uv2.Count == n; c.HasT = c.Tangents.Count == n; c.HasC = c.Colors.Count == n;
                for (int s = 0; s < src.subMeshCount; s++) c.Subs.Add(new List<int>(src.GetTriangles(s)));
                return c;
            }

            /// <summary>Appends a vertex lerped from a to b (t = 0 copies a).</summary>
            public int Lerp(int a, int b, float t)
            {
                Verts.Add(Vector3.Lerp(Verts[a], Verts[b], t));
                if (HasN) Normals.Add(Vector3.Lerp(Normals[a], Normals[b], t).normalized);
                if (HasUv) Uv.Add(Vector2.Lerp(Uv[a], Uv[b], t));
                if (HasUv2) Uv2.Add(Vector2.Lerp(Uv2[a], Uv2[b], t));
                if (HasT) Tangents.Add(Vector4.Lerp(Tangents[a], Tangents[b], t));
                if (HasC) Colors.Add(Color.Lerp(Colors[a], Colors[b], t));
                return Verts.Count - 1;
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(Verts);
                if (HasN) mesh.SetNormals(Normals);
                if (HasUv) mesh.SetUVs(0, Uv);
                if (HasUv2) mesh.SetUVs(1, Uv2);
                if (HasT) mesh.SetTangents(Tangents);
                if (HasC) mesh.SetColors(Colors);
                mesh.subMeshCount = Subs.Count;
                for (int s = 0; s < Subs.Count; s++) mesh.SetTriangles(Subs[s], s);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>
        /// Clips every triangle against the planes x, y, z = k * <see cref="CellMetres"/> (in metres, through <paramref name="toWorld"/>)
        /// and fan-triangulates the pieces. <paramref name="clusters"/> gets the grid cell of each output triangle's centroid as
        /// consecutive ids, per triangle in submesh order. A vertex cut from an edge is shared by every triangle on that edge.
        /// </summary>
        public static Mesh ClipToGrid(Mesh src, Matrix4x4 toWorld, out int[] clusters)
        {
            Channels ch = Channels.Read(src);
            var world = new List<Vector3>(ch.Verts.Count);
            foreach (Vector3 v in ch.Verts) world.Add(toWorld.MultiplyPoint3x4(v));

            var cut = new Dictionary<(int, int, int, int), int>();
            int Cut(int a, int b, int axis, int plane)
            {
                if (a > b) (a, b) = (b, a); // same endpoint order, so both neighbours compute the same vertex
                var key = (a, b, axis, plane);
                if (cut.TryGetValue(key, out int m)) return m;
                float c = plane * CellMetres;
                float t = (c - world[a][axis]) / (world[b][axis] - world[a][axis]);
                m = ch.Lerp(a, b, t);
                Vector3 w = Vector3.Lerp(world[a], world[b], t);
                w[axis] = c;
                world.Add(w);
                cut[key] = m;
                return m;
            }

            var cellId = new Dictionary<Vector3Int, int>();
            var outClusters = new List<int>();
            var stack = new Stack<List<int>>();
            for (int s = 0; s < ch.Subs.Count; s++)
            {
                List<int> old = ch.Subs[s];
                var next = new List<int>(old.Count * 2);
                for (int i = 0; i < old.Count; i += 3)
                {
                    stack.Push(new List<int> { old[i], old[i + 1], old[i + 2] });
                    while (stack.Count > 0)
                    {
                        List<int> poly = stack.Pop();
                        int axis, plane = 0;
                        bool found = false;
                        for (axis = 0; axis < 3 && !found; axis++)
                        {
                            float lo = float.MaxValue, hi = float.MinValue;
                            foreach (int v in poly) { lo = Mathf.Min(lo, world[v][axis]); hi = Mathf.Max(hi, world[v][axis]); }
                            plane = Mathf.FloorToInt(lo / CellMetres) + 1;
                            found = plane * CellMetres < hi;
                            if (found) break;
                        }
                        if (found)
                        {
                            // Vertices on the plane go to both halves.
                            var below = new List<int>(); var above = new List<int>();
                            float c = plane * CellMetres;
                            for (int k = 0; k < poly.Count; k++)
                            {
                                int a = poly[k], b = poly[(k + 1) % poly.Count];
                                float da = world[a][axis] - c, db = world[b][axis] - c;
                                if (da <= 0f) below.Add(a);
                                if (da >= 0f) above.Add(a);
                                if (da < 0f && db > 0f || da > 0f && db < 0f)
                                {
                                    int m = Cut(a, b, axis, plane);
                                    below.Add(m); above.Add(m);
                                }
                            }
                            if (below.Count >= 3) stack.Push(below);
                            if (above.Count >= 3) stack.Push(above);
                            continue;
                        }
                        for (int k = 1; k + 1 < poly.Count; k++)
                        {
                            int a = poly[0], b = poly[k], c = poly[k + 1];
                            if (Vector3.Cross(world[b] - world[a], world[c] - world[a]).sqrMagnitude < 1e-12f) continue; // a sliver left by a vertex on a plane
                            next.Add(a); next.Add(b); next.Add(c);
                            Vector3 centre = (world[a] + world[b] + world[c]) / 3f;
                            var cell = new Vector3Int(Mathf.FloorToInt(centre.x / CellMetres), Mathf.FloorToInt(centre.y / CellMetres), Mathf.FloorToInt(centre.z / CellMetres));
                            if (!cellId.TryGetValue(cell, out int id)) cellId[cell] = id = cellId.Count;
                            outClusters.Add(id);
                        }
                    }
                }
                ch.Subs[s] = next;
            }
            clusters = outClusters.ToArray();
            return ch.ToMesh();
        }

        /// <summary>
        /// Welds vertices closer than <see cref="WeldMetres"/> (in metres, through <paramref name="toWorld"/>): returns for each vertex the
        /// index of its first welded vertex. Neighbouring hash cells are searched, so points straddling a cell border still weld.
        /// </summary>
        private static int[] WeldVertices(List<Vector3> verts, Matrix4x4 toWorld)
        {
            var cells = new Dictionary<Vector3Int, List<int>>();
            var world = new Vector3[verts.Count];
            var weld = new int[verts.Count];
            for (int v = 0; v < weld.Length; v++)
            {
                Vector3 p = world[v] = toWorld.MultiplyPoint3x4(verts[v]);
                var cell = new Vector3Int(Mathf.FloorToInt(p.x / WeldMetres), Mathf.FloorToInt(p.y / WeldMetres), Mathf.FloorToInt(p.z / WeldMetres));
                weld[v] = v;
                for (int x = -1; x <= 1 && weld[v] == v; x++)
                    for (int y = -1; y <= 1 && weld[v] == v; y++)
                        for (int z = -1; z <= 1 && weld[v] == v; z++)
                            if (cells.TryGetValue(cell + new Vector3Int(x, y, z), out List<int> list))
                                foreach (int r in list)
                                    if ((world[r] - p).sqrMagnitude <= WeldMetres * WeldMetres) { weld[v] = r; break; }
                if (weld[v] == v)
                {
                    if (!cells.TryGetValue(cell, out List<int> own)) cells[cell] = own = new List<int>();
                    own.Add(v);
                }
            }
            return weld;
        }

        /// <summary>Props are not clipped: a copy of <paramref name="src"/> where each connected island (welded at <see cref="WeldMetres"/>) is one cluster.</summary>
        public static Mesh ClusterIslands(Mesh src, Matrix4x4 toWorld, out int[] clusters)
        {
            Channels ch = Channels.Read(src);
            int[] weld = WeldVertices(ch.Verts, toWorld);
            var parent = new int[weld.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            foreach (List<int> tris in ch.Subs)
                for (int i = 0; i < tris.Count; i += 3)
                {
                    parent[Find(weld[tris[i + 1]])] = Find(weld[tris[i]]);
                    parent[Find(weld[tris[i + 2]])] = Find(weld[tris[i]]);
                }
            var idOf = new Dictionary<int, int>();
            var list = new List<int>();
            foreach (List<int> tris in ch.Subs)
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int root = Find(weld[tris[i]]);
                    if (!idOf.TryGetValue(root, out int id)) idOf[root] = id = idOf.Count;
                    list.Add(id);
                }
            clusters = list.ToArray();
            return ch.ToMesh();
        }

        // Castle parts live at 1/100 size; same guard as FracturableObject.Direction.
        private static Vector3 Direction(Vector3 v)
        {
            Vector3 scaled = v * 1e6f;
            return scaled.sqrMagnitude > 1e-12f ? scaled.normalized : Vector3.up;
        }

        /// <summary>
        /// A falling cluster carries its edge (#490). On the double-sided <paramref name="src"/>, each run of touching, parallel
        /// border edges (front triangles only) of one cluster gets one single-sided quad of the same submesh and cluster, running
        /// <paramref name="depth"/> (mesh units) inward along the first triangle's negative normal and facing away from its cluster.
        /// <paramref name="clusters"/> and <paramref name="kinds"/> (0 front, 1 back twin, 2 stone edge) are per triangle, in
        /// submesh order, and come back extended to the new triangle order.
        /// </summary>
        public static Mesh AddStoneEdges(Mesh src, Matrix4x4 toWorld, ref int[] clusters, ref byte[] kinds, float depth)
        {
            Channels ch = Channels.Read(src);
            int[] weld = WeldVertices(ch.Verts, toWorld);

            var tris = new List<int>(); var sub = new List<int>();
            for (int s = 0; s < ch.Subs.Count; s++)
                for (int i = 0; i < ch.Subs[s].Count; i += 3)
                {
                    tris.Add(ch.Subs[s][i]); tris.Add(ch.Subs[s][i + 1]); tris.Add(ch.Subs[s][i + 2]);
                    sub.Add(s);
                }

            (int, int) EdgeKey(int a, int b) => weld[a] < weld[b] ? (weld[a], weld[b]) : (weld[b], weld[a]);
            var onEdge = new Dictionary<(int, int), List<int>>();
            for (int t = 0; t < sub.Count; t++)
            {
                if (kinds[t] != 0) continue;
                for (int e = 0; e < 3; e++)
                {
                    var key = EdgeKey(tris[t * 3 + e], tris[t * 3 + (e + 1) % 3]);
                    if (!onEdge.TryGetValue(key, out List<int> list)) onEdge[key] = list = new List<int>();
                    list.Add(t);
                }
            }

            // Edges on a cluster border, or with no neighbour.
            var border = new List<(int cluster, int tri, int a, int b)>();
            if (depth > 0f)
                for (int t = 0; t < sub.Count; t++)
                {
                    if (kinds[t] != 0) continue;
                    for (int e = 0; e < 3; e++)
                    {
                        int ia = tris[t * 3 + e], ib = tris[t * 3 + (e + 1) % 3];
                        List<int> sharing = onEdge[EdgeKey(ia, ib)];
                        bool isBorder = sharing.Count == 1;
                        foreach (int o in sharing) if (clusters[o] != clusters[t]) isBorder = true;
                        if (isBorder) border.Add((clusters[t], t, ia, ib));
                    }
                }

            // Touching, parallel border edges of one cluster make a run.
            var parent = new int[border.Count];
            for (int i = 0; i < parent.Length; i++) parent[i] = i;
            int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
            Vector3 Dir(int i) => toWorld.MultiplyVector(ch.Verts[border[i].b] - ch.Verts[border[i].a]).normalized;
            var atVertex = new Dictionary<(int, int), List<int>>();
            for (int i = 0; i < border.Count; i++)
                foreach (int v in new[] { border[i].a, border[i].b })
                {
                    var key = (border[i].cluster, weld[v]);
                    if (!atVertex.TryGetValue(key, out List<int> l)) atVertex[key] = l = new List<int>();
                    l.Add(i);
                }
            foreach (List<int> l in atVertex.Values)
                for (int x = 0; x < l.Count; x++)
                    for (int y = x + 1; y < l.Count; y++)
                        if (Vector3.Cross(Dir(l[x]), Dir(l[y])).sqrMagnitude < 1e-6f) parent[Find(l[x])] = Find(l[y]);
            var runs = new Dictionary<int, List<int>>();
            for (int i = 0; i < border.Count; i++)
            {
                int root = Find(i);
                if (!runs.TryGetValue(root, out List<int> run)) runs[root] = run = new List<int>();
                run.Add(i);
            }

            // Many upper floors and roofs are 2 cm slabs, not 30 cm: a full-depth edge from one face poked through the other as
            // a ridge along every cluster border (aj, 2026-10-28). Each edge stops half way to the opposite face of its slab.
            var probe = new GameObject("StoneEdgeDepthProbe") { hideFlags = HideFlags.HideAndDontSave };
            probe.transform.SetPositionAndRotation(toWorld.GetPosition(), toWorld.rotation);
            probe.transform.localScale = toWorld.lossyScale;
            var probeCollider = probe.AddComponent<MeshCollider>();
            probeCollider.sharedMesh = src;
            float metresPerUnit = toWorld.lossyScale.x;

            var newTris = new List<int>[ch.Subs.Count]; var newClusters = new List<int>[ch.Subs.Count]; var newKinds = new List<byte>[ch.Subs.Count];
            int at = 0;
            for (int s = 0; s < ch.Subs.Count; s++)
            {
                newTris[s] = new List<int>(ch.Subs[s]); newClusters[s] = new List<int>(); newKinds[s] = new List<byte>();
                for (int i = 0; i < ch.Subs[s].Count / 3; i++) { newClusters[s].Add(clusters[at]); newKinds[s].Add(kinds[at]); at++; }
            }

            foreach (List<int> run in runs.Values)
            {
                var rep = border[run[0]];
                Vector3 d = Dir(run[0]);
                int ia = -1, ib = -1;
                float lo = float.MaxValue, hi = float.MinValue;
                foreach (int i in run)
                    foreach (int v in new[] { border[i].a, border[i].b })
                    {
                        float proj = Vector3.Dot(toWorld.MultiplyPoint3x4(ch.Verts[v]), d);
                        if (proj < lo) { lo = proj; ia = v; }
                        if (proj > hi) { hi = proj; ib = v; }
                    }
                int t = rep.tri;
                Vector3 p0 = ch.Verts[tris[t * 3]], p1 = ch.Verts[tris[t * 3 + 1]], p2 = ch.Verts[tris[t * 3 + 2]];
                Vector3 inwardDir = -Direction(Vector3.Cross(p1 - p0, p2 - p0));
                Vector3 a = ch.Verts[ia], b = ch.Verts[ib];
                Vector3 inward = inwardDir * SlabHalfDepth(probeCollider, toWorld, (a + b) * 0.5f, inwardDir, depth, metresPerUnit);
                Vector3 centroid = (p0 + p1 + p2) / 3f;
                Vector3 w = Vector3.Cross(b - a, inward);
                bool flip = Vector3.Dot(w, centroid - a) > 0f; // the front face points away from the cluster, toward the hole
                Vector3 normal = Direction(flip ? -w : w);
                int q = ch.Verts.Count;
                ch.Lerp(ia, ia, 0f); ch.Lerp(ib, ib, 0f); ch.Lerp(ib, ib, 0f); ch.Lerp(ia, ia, 0f); // uvs and colours from the run's end vertices
                ch.Verts[q + 2] = b + inward; ch.Verts[q + 3] = a + inward;
                if (ch.HasN) for (int k = 0; k < 4; k++) ch.Normals[q + k] = normal;
                int[] order = flip ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
                foreach (int o in order) newTris[sub[t]].Add(q + o);
                for (int k = 0; k < 2; k++) { newClusters[sub[t]].Add(rep.cluster); newKinds[sub[t]].Add(2); }
            }

            Object.DestroyImmediate(probe);

            var allClusters = new List<int>(); var allKinds = new List<byte>();
            for (int s = 0; s < ch.Subs.Count; s++) { ch.Subs[s] = newTris[s]; allClusters.AddRange(newClusters[s]); allKinds.AddRange(newKinds[s]); }
            clusters = allClusters.ToArray();
            kinds = allKinds.ToArray();
            return ch.ToMesh();
        }

        /// <summary>The stone edge's depth (mesh units) at <paramref name="point"/>: <paramref name="maxDepth"/>, or half the
        /// distance to the slab's opposite face along <paramref name="inwardDir"/> when that face is nearer than twice it.</summary>
        public static float SlabHalfDepth(MeshCollider slab, Matrix4x4 toWorld, Vector3 point, Vector3 inwardDir, float maxDepth, float metresPerUnit)
        {
            Vector3 dirWorld = toWorld.MultiplyVector(inwardDir).normalized;
            // Starts 1 mm inside, so the face the edge hangs from (and its twin) is never the hit.
            Vector3 from = toWorld.MultiplyPoint3x4(point) + dirWorld * 0.001f;
            float reach = maxDepth * 2f * metresPerUnit;
            bool hitsBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            bool hit = slab.Raycast(new Ray(from, dirWorld), out RaycastHit h, reach);
            Physics.queriesHitBackfaces = hitsBackfaces;
            return hit ? Mathf.Min(maxDepth, (h.distance + 0.001f) * 0.5f / metresPerUnit) : maxDepth;
        }
    }
}

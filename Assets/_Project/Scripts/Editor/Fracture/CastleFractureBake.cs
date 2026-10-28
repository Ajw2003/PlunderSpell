using System.Collections.Generic;
using Plunderspell.Fracture;
using Plunderspell.Fracture.Editor;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Bakes the fracture data for each castle room part (#472): subdivides the split part meshes (#471) to short edges so
    /// a hit breaks off small pieces, makes them double-sided, saves a new mesh asset, and bakes the FracturableObject
    /// graph. Always starts from the split output, so it can be rerun.
    /// </summary>
    public static class CastleFractureBake
    {
        private const string PartDir = "Assets/_Project/Art/Models/Castle/Parts";
        private const string FractureDir = PartDir + "/Fracture";
        private const float MaxEdgeMetres = 0.75f;
        private const float WeldMetres = 0.01f;
        private const int MaxPasses = 12;

        [MenuItem("Tools/Plunderspell/Castle/Bake Castle Room Fracture")]
        public static void BakeMenu()
        {
            if (!AssetDatabase.IsValidFolder(FractureDir)) AssetDatabase.CreateFolder(PartDir, "Fracture");
            foreach (string path in RoomPartSplitter.RoomPrefabPaths())
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

                        Mesh sub = Subdivide(source, part.localToWorldMatrix);
                        Mesh mesh = DoubleSidedMeshGenerator.CreateDoubleSidedMesh(sub);
                        Object.DestroyImmediate(sub);
                        mesh.name = $"{root.name}_{name}_Fracture";
                        string meshPath = $"{FractureDir}/{mesh.name}.asset";
                        AssetDatabase.DeleteAsset(meshPath); // a fresh asset every time, never overwritten in place
                        AssetDatabase.CreateAsset(mesh, meshPath);

                        part.GetComponent<MeshFilter>().sharedMesh = mesh;
                        part.GetComponent<MeshCollider>().sharedMesh = mesh;
                        if (!part.TryGetComponent(out FracturableObject fo)) fo = part.gameObject.AddComponent<FracturableObject>();
                        fo.destroyWhenEmpty = false;
                        fo.preInitialize = false;
                        // The castle's own shader keeps drawing what is left; the fracture shader draws only the debris.
                        fo.keepMeshRenderer = true;
                        // mergeDistance is in the mesh's own space; the rooms are stored at 1/100 size, so the 0.05 default
                        // welded everything within 5 m and the graph build ran out of memory. 1 cm in metres instead.
                        fo.mergeDistance = WeldMetres / part.lossyScale.x;
                        fo.BakeFractureGraph();
                        log += $" {name}={source.triangles.Length / 3}->{mesh.triangles.Length / 3}";
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[CastleFracture] {root.name}: triangles before->after{log}");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Splits every edge longer than <see cref="MaxEdgeMetres"/> (measured through <paramref name="toWorld"/>) at its midpoint, repeating until none is. Submeshes and vertex channels are kept.</summary>
        private static Mesh Subdivide(Mesh src, Matrix4x4 toWorld)
        {
            var verts = new List<Vector3>(); src.GetVertices(verts);
            int n = verts.Count;
            var normals = new List<Vector3>(); src.GetNormals(normals);
            var uv = new List<Vector2>(); src.GetUVs(0, uv);
            var uv2 = new List<Vector2>(); src.GetUVs(1, uv2);
            var tangents = new List<Vector4>(); src.GetTangents(tangents);
            var colors = new List<Color>(); src.GetColors(colors);
            bool hasN = normals.Count == n, hasUv = uv.Count == n, hasUv2 = uv2.Count == n, hasT = tangents.Count == n, hasC = colors.Count == n;

            var subs = new List<List<int>>();
            for (int s = 0; s < src.subMeshCount; s++) subs.Add(new List<int>(src.GetTriangles(s)));

            long Key(int a, int b) => a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                var mid = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long k = Key(a, b);
                    if (mid.TryGetValue(k, out int m)) return m;
                    m = verts.Count;
                    verts.Add((verts[a] + verts[b]) * 0.5f);
                    if (hasN) normals.Add(((normals[a] + normals[b]) * 0.5f).normalized);
                    if (hasUv) uv.Add((uv[a] + uv[b]) * 0.5f);
                    if (hasUv2) uv2.Add((uv2[a] + uv2[b]) * 0.5f);
                    if (hasT) tangents.Add((tangents[a] + tangents[b]) * 0.5f);
                    if (hasC) colors.Add(Color.Lerp(colors[a], colors[b], 0.5f));
                    mid[k] = m;
                    return m;
                }

                // Mark long edges first so neighbours split a shared edge identically (no cracks).
                var marked = new HashSet<long>();
                foreach (var tris in subs)
                    for (int i = 0; i < tris.Count; i += 3)
                        for (int e = 0; e < 3; e++)
                        {
                            int a = tris[i + e], b = tris[i + (e + 1) % 3];
                            if (toWorld.MultiplyVector(verts[a] - verts[b]).magnitude > MaxEdgeMetres) marked.Add(Key(a, b));
                        }
                if (marked.Count == 0) break;

                for (int s = 0; s < subs.Count; s++)
                {
                    var old = subs[s];
                    var next = new List<int>(old.Count * 2);
                    for (int i = 0; i < old.Count; i += 3)
                    {
                        // Polygon around the triangle with a midpoint after each marked edge.
                        var poly = new List<int>(6);
                        for (int e = 0; e < 3; e++)
                        {
                            int a = old[i + e], b = old[i + (e + 1) % 3];
                            poly.Add(a);
                            if (marked.Contains(Key(a, b))) poly.Add(Mid(a, b));
                        }
                        if (poly.Count == 3)
                        {
                            next.Add(old[i]); next.Add(old[i + 1]); next.Add(old[i + 2]);
                            continue;
                        }
                        if (poly.Count == 6)
                        {
                            // All three marked: the even 4-way split, not a sliver fan.
                            next.AddRange(new[] { poly[0], poly[1], poly[5], poly[1], poly[2], poly[3], poly[5], poly[3], poly[4], poly[1], poly[3], poly[5] });
                            continue;
                        }
                        // One or two marked edges: cut so no new triangle keeps a marked (long) edge. A plain fan from a corner
                        // reused a long edge with two marks, so it never shrank and the triangle count ran away.
                        int v0 = old[i], v1 = old[i + 1], v2 = old[i + 2];
                        bool m01 = marked.Contains(Key(v0, v1)), m12 = marked.Contains(Key(v1, v2)), m20 = marked.Contains(Key(v2, v0));
                        // Rotate so the corners are (a, b, c) with the marked edges first: a-b always marked, b-c marked only for two.
                        int ra, rb, rc;
                        if (m01 && m12 || m01 && !m20) { ra = v0; rb = v1; rc = v2; }
                        else if (m12 && m20 || m12) { ra = v1; rb = v2; rc = v0; }
                        else { ra = v2; rb = v0; rc = v1; }
                        int mab = Mid(ra, rb);
                        if (marked.Contains(Key(rb, rc)))
                        {
                            int mbc = Mid(rb, rc);
                            next.AddRange(new[] { mab, rb, mbc, ra, mab, mbc, ra, mbc, rc });
                        }
                        else next.AddRange(new[] { ra, mab, rc, mab, rb, rc });
                    }
                    subs[s] = next;
                }
            }

            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            if (hasN) mesh.SetNormals(normals);
            if (hasUv) mesh.SetUVs(0, uv);
            if (hasUv2) mesh.SetUVs(1, uv2);
            if (hasT) mesh.SetTangents(tangents);
            if (hasC) mesh.SetColors(colors);
            mesh.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) mesh.SetTriangles(subs[s], s);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

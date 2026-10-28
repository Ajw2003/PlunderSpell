using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Cuts the Ophan's charged beam through the castle room meshes (#467): triangles near the beam's line are split
    /// small, and the ones inside the beam are dropped, so the hole is real for the eye and the collider alike, on every
    /// machine. Always works on a copy; a room's mesh asset is shared by every instance of that room.
    /// </summary>
    public static class OphanWallCut
    {
        public const float Radius = 1.5f;
        public const float MaxEdge = 0.5f;

        // Cut copies this class made, the only meshes it may destroy.
        private static readonly HashSet<Mesh> s_made = new HashSet<Mesh>();

        /// <summary>Cuts the beam's hole in every mesh of the named rooms. Rooms that do not exist here are skipped.</summary>
        public static void CutRooms(int[] roomIndices, Vector3 from, Vector3 to)
        {
            foreach (int index in roomIndices)
            {
                var room = ScorchedRooms.RoomAt(index);
                if (room == null)
                    continue;
                foreach (MeshFilter filter in room.GetComponentsInChildren<MeshFilter>())
                {
                    Mesh old = filter.sharedMesh;
                    Mesh cut = Cut(old, filter.transform.localToWorldMatrix, from, to, Radius, MaxEdge);
                    if (cut == null)
                        continue;
                    filter.sharedMesh = cut;
                    foreach (MeshCollider collider in filter.GetComponents<MeshCollider>())
                        if (collider.sharedMesh == old)
                            collider.sharedMesh = cut;
                    s_made.Add(cut);
                    if (s_made.Remove(old))
                        Object.Destroy(old);
                }
            }
        }

        /// <summary>A new mesh like <paramref name="source"/> with the beam's segment cut out of it; the source is untouched.</summary>
        public static Mesh Cut(Mesh source, Matrix4x4 localToWorld, Vector3 from, Vector3 to, float radius, float maxEdge)
        {
            if (source == null || !source.isReadable)
            {
                Debug.LogError("[Ophan] wall cut needs a readable mesh.");
                return null;
            }
            var pos = new List<Vector3>(source.vertices);
            int count = pos.Count;
            var nor = new List<Vector3>(source.normals);
            var tan = new List<Vector4>(source.tangents);
            var uv = new List<Vector2>(source.uv);
            var uv2 = new List<Vector2>(source.uv2);
            var col = new List<Color>(source.colors);
            bool hasNor = nor.Count == count, hasTan = tan.Count == count, hasUv = uv.Count == count;
            bool hasUv2 = uv2.Count == count, hasCol = col.Count == count;

            int Mid(int a, int b)
            {
                pos.Add((pos[a] + pos[b]) * 0.5f);
                if (hasNor) nor.Add(((nor[a] + nor[b]) * 0.5f).normalized);
                if (hasTan)
                {
                    Vector4 t = (tan[a] + tan[b]) * 0.5f;
                    tan.Add(new Vector4(t.x, t.y, t.z, tan[a].w));
                }
                if (hasUv) uv.Add((uv[a] + uv[b]) * 0.5f);
                if (hasUv2) uv2.Add((uv2[a] + uv2[b]) * 0.5f);
                if (hasCol) col.Add(Color.Lerp(col[a], col[b], 0.5f));
                return pos.Count - 1;
            }

            Vector3 segMin = Vector3.Min(from, to) - Vector3.one * radius;
            Vector3 segMax = Vector3.Max(from, to) + Vector3.one * radius;
            var result = new Mesh { name = source.name + "_cut", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            var stack = new Stack<(int, int, int)>();
            var kept = new List<int>();
            var subs = new List<List<int>>();
            for (int s = 0; s < source.subMeshCount; s++)
            {
                kept = new List<int>();
                int[] tris = source.GetTriangles(s);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    stack.Push((tris[t], tris[t + 1], tris[t + 2]));
                    while (stack.Count > 0)
                    {
                        (int a, int b, int c) = stack.Pop();
                        Vector3 wa = localToWorld.MultiplyPoint3x4(pos[a]);
                        Vector3 wb = localToWorld.MultiplyPoint3x4(pos[b]);
                        Vector3 wc = localToWorld.MultiplyPoint3x4(pos[c]);
                        // Bounds first: a triangle whose box (plus radius) misses the beam's box cannot be touched.
                        bool near = Vector3.Min(wa, Vector3.Min(wb, wc)).x <= segMax.x && Vector3.Max(wa, Vector3.Max(wb, wc)).x >= segMin.x
                            && Vector3.Min(wa, Vector3.Min(wb, wc)).y <= segMax.y && Vector3.Max(wa, Vector3.Max(wb, wc)).y >= segMin.y
                            && Vector3.Min(wa, Vector3.Min(wb, wc)).z <= segMax.z && Vector3.Max(wa, Vector3.Max(wb, wc)).z >= segMin.z;
                        float longest = Mathf.Max((wa - wb).magnitude, Mathf.Max((wb - wc).magnitude, (wc - wa).magnitude));
                        float dist = near ? DistanceToSegment((wa + wb + wc) / 3f, from, to) : float.MaxValue;
                        if (dist >= radius + longest || (longest <= maxEdge && dist >= radius))
                        {
                            kept.Add(a); kept.Add(b); kept.Add(c);
                        }
                        else if (longest > maxEdge)
                        {
                            int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                            stack.Push((a, ab, ca)); stack.Push((ab, b, bc)); stack.Push((ca, bc, c)); stack.Push((ab, bc, ca));
                        }
                    }
                }
                subs.Add(kept);
            }
            result.SetVertices(pos);
            if (hasNor) result.SetNormals(nor);
            if (hasTan) result.SetTangents(tan);
            if (hasUv) result.SetUVs(0, uv);
            if (hasUv2) result.SetUVs(1, uv2);
            if (hasCol) result.SetColors(col);
            result.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++)
                result.SetTriangles(subs[s], s);
            result.RecalculateBounds();
            return result;
        }

        private static float DistanceToSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            float t = len2 > 0f ? Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2) : 0f;
            return Vector3.Distance(p, a + ab * t);
        }
    }
}

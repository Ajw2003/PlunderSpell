using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// The iron that holds each fire, built from turned profiles and boxes: a brazier's bowl on a
    /// pole, a wall bracket with its torch, a beacon's basket. Flat-shaded and chunky, like the
    /// castle kit. Y up, base at the origin; a sconce's wall is behind it (-Z).
    /// </summary>
    public static class FireMeshes
    {
        public static Mesh Brazier()
        {
            var b = new Builder();
            b.Lathe(new[] { new Vector2(0.001f, 0f), new Vector2(0.32f, 0f), new Vector2(0.28f, 0.06f),
                new Vector2(0.07f, 0.1f), new Vector2(0.001f, 0.1f) }, 8);
            b.Lathe(new[] { new Vector2(0.001f, 0.08f), new Vector2(0.055f, 0.08f), new Vector2(0.05f, 1.15f),
                new Vector2(0.001f, 1.15f) }, 6);
            // The bowl: flared, with a lip, open on top where the fire sits.
            b.Lathe(new[] { new Vector2(0.001f, 1.1f), new Vector2(0.14f, 1.12f), new Vector2(0.4f, 1.36f),
                new Vector2(0.46f, 1.44f), new Vector2(0.42f, 1.46f), new Vector2(0.36f, 1.38f),
                new Vector2(0.001f, 1.3f) }, 10);
            return b.ToMesh();
        }

        public static Mesh Sconce()
        {
            var b = new Builder();
            b.Box(new Vector3(0f, 0f, 0.03f), new Vector3(0.16f, 0.34f, 0.06f));             // wall plate
            b.Box(new Vector3(0f, -0.02f, 0.17f), new Vector3(0.05f, 0.05f, 0.26f));          // arm
            b.Lathe(new[] { new Vector2(0.001f, 0f), new Vector2(0.06f, 0f), new Vector2(0.1f, 0.12f),
                new Vector2(0.001f, 0.12f) }, 6, new Vector3(0f, 0.03f, 0.34f));             // cup
            b.Lathe(new[] { new Vector2(0.001f, 0f), new Vector2(0.035f, 0f), new Vector2(0.045f, 0.32f),
                new Vector2(0.001f, 0.32f) }, 6, new Vector3(0f, 0.06f, 0.34f));             // torch haft
            return b.ToMesh();
        }

        public static Mesh Beacon()
        {
            var b = new Builder();
            // A squat iron basket on three stubby legs.
            for (int i = 0; i < 3; i++)
            {
                float a = i * Mathf.PI * 2f / 3f;
                b.Box(new Vector3(Mathf.Cos(a) * 0.55f, 0.2f, Mathf.Sin(a) * 0.55f), new Vector3(0.1f, 0.4f, 0.1f));
            }
            b.Lathe(new[] { new Vector2(0.001f, 0.38f), new Vector2(0.5f, 0.38f), new Vector2(0.85f, 0.78f),
                new Vector2(0.92f, 0.86f), new Vector2(0.84f, 0.88f), new Vector2(0.72f, 0.74f),
                new Vector2(0.001f, 0.6f) }, 12);
            return b.ToMesh();
        }

        /// <summary>Accumulates flat-shaded triangles.</summary>
        private class Builder
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<int> _triangles = new List<int>();

            public void Box(Vector3 centre, Vector3 size)
            {
                Vector3 h = size * 0.5f;
                Vector3[] c =
                {
                    centre + new Vector3(-h.x, -h.y, -h.z), centre + new Vector3(h.x, -h.y, -h.z),
                    centre + new Vector3(h.x, h.y, -h.z), centre + new Vector3(-h.x, h.y, -h.z),
                    centre + new Vector3(-h.x, -h.y, h.z), centre + new Vector3(h.x, -h.y, h.z),
                    centre + new Vector3(h.x, h.y, h.z), centre + new Vector3(-h.x, h.y, h.z),
                };
                Quad(c[0], c[3], c[2], c[1]);
                Quad(c[5], c[6], c[7], c[4]);
                Quad(c[4], c[7], c[3], c[0]);
                Quad(c[1], c[2], c[6], c[5]);
                Quad(c[3], c[7], c[6], c[2]);
                Quad(c[4], c[0], c[1], c[5]);
            }

            /// <summary>Turns a (radius, height) profile about Y. Profile runs bottom to top.</summary>
            public void Lathe(Vector2[] profile, int segments, Vector3 offset = default)
            {
                for (int s = 0; s < segments; s++)
                {
                    float a0 = s * Mathf.PI * 2f / segments;
                    float a1 = (s + 1) * Mathf.PI * 2f / segments;
                    for (int p = 0; p < profile.Length - 1; p++)
                    {
                        Vector3 v00 = offset + Point(profile[p], a0);
                        Vector3 v01 = offset + Point(profile[p], a1);
                        Vector3 v10 = offset + Point(profile[p + 1], a0);
                        Vector3 v11 = offset + Point(profile[p + 1], a1);
                        Quad(v00, v10, v11, v01);
                    }
                }
            }

            private static Vector3 Point(Vector2 rh, float angle) =>
                new Vector3(Mathf.Cos(angle) * rh.x, rh.y, Mathf.Sin(angle) * rh.x);

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                Triangle(a, b, c);
                Triangle(a, c, d);
            }

            private void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 normal = Vector3.Cross(b - a, c - a);
                if (normal.sqrMagnitude < 1e-12f)
                    return;
                normal.Normalize();
                int start = _vertices.Count;
                _vertices.Add(a);
                _vertices.Add(b);
                _vertices.Add(c);
                _normals.Add(normal);
                _normals.Add(normal);
                _normals.Add(normal);
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
            }

            public Mesh ToMesh()
            {
                var mesh = new Mesh();
                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, new List<Vector2>(new Vector2[_vertices.Count]));
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}

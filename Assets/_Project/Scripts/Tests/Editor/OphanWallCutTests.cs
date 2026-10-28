using NUnit.Framework;
using Plunderspell.Raid;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    public class OphanWallCutTests
    {
        // A 10 m x 10 m quad in the XZ plane, centred on the origin.
        private static Mesh Quad()
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { new Vector3(-5, 0, -5), new Vector3(-5, 0, 5), new Vector3(5, 0, 5), new Vector3(5, 0, -5) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }

        private static Vector3 Centroid(Mesh m, int t)
        {
            int[] i = m.triangles;
            Vector3[] v = m.vertices;
            return (v[i[t * 3]] + v[i[t * 3 + 1]] + v[i[t * 3 + 2]]) / 3f;
        }

        [Test]
        public void CutThroughTheMiddleLeavesNoTriangleInTheBeamAndKeepsTheCorners()
        {
            Mesh source = Quad();
            Mesh cut = OphanWallCut.Cut(source, Matrix4x4.identity, new Vector3(0, -5, 0), new Vector3(0, 5, 0), 1.5f, 0.5f);
            bool corner = false;
            float area = 0f;
            int[] i = cut.triangles;
            Vector3[] v = cut.vertices;
            for (int t = 0; t < i.Length / 3; t++)
            {
                Vector3 c = Centroid(cut, t);
                Assert.GreaterOrEqual(new Vector2(c.x, c.z).magnitude, 1.5f, "No piece is left inside the beam.");
                corner |= c.x > 3f && c.z > 3f;
                area += Vector3.Cross(v[i[t * 3 + 1]] - v[i[t * 3]], v[i[t * 3 + 2]] - v[i[t * 3]]).magnitude * 0.5f;
            }
            Assert.IsTrue(corner, "The far corner is kept.");
            // A 1.5 m hole is about 7 m2 of the 100 m2; 0.5 m pieces make its edge ragged by a piece or so.
            Assert.That(area, Is.InRange(85f, 95f), "Only about the hole's area is removed.");
            Assert.AreEqual(2, source.triangles.Length / 3);
        }

        [Test]
        public void ASegmentThatMissesChangesNothing()
        {
            Mesh cut = OphanWallCut.Cut(Quad(), Matrix4x4.identity, new Vector3(0, 5, 0), new Vector3(0, 10, 0), 1.5f, 0.5f);
            Assert.AreEqual(2, cut.triangles.Length / 3);
        }
    }
}

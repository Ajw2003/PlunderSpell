using NUnit.Framework;
using Plunderspell.EditorTools;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>The 1.5 m grid clip of the castle fracture bake (#490).</summary>
    public class CastleFractureClusterTests
    {
        [Test]
        public void QuadThreeMetresWideClipsIntoTwoCellsAndNothingCrossesThePlane()
        {
            var src = new Mesh();
            src.vertices = new[] { new Vector3(0.1f, 0.1f, 0f), new Vector3(3.1f, 0.1f, 0f), new Vector3(3.1f, 1f, 0f), new Vector3(0.1f, 1f, 0f) };
            src.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            Mesh clipped = CastleFractureBake.ClipToGrid(src, Matrix4x4.identity, out int[] clusters);

            int[] tris = clipped.triangles;
            Assert.AreEqual(tris.Length / 3, clusters.Length);
            Assert.AreEqual(3, new System.Collections.Generic.HashSet<int>(clusters).Count, "cells x 0, 1 and 2 (3.1 m wide from 0.1)");
            Vector3[] v = clipped.vertices;
            for (int i = 0; i < tris.Length; i += 3)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (int k = 0; k < 3; k++) { lo = Mathf.Min(lo, v[tris[i + k]].x); hi = Mathf.Max(hi, v[tris[i + k]].x); }
                Assert.That(Mathf.FloorToInt(lo / 1.5f) == Mathf.FloorToInt((hi - 1e-4f) / 1.5f), $"triangle {i / 3} crosses a plane");
            }
            Object.DestroyImmediate(src); Object.DestroyImmediate(clipped);
        }
    }
}

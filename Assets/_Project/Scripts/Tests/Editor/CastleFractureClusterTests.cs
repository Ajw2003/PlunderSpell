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

        [Test]
        public void StoneEdgeOnATwoCentimetreSlabStopsHalfWayThrough()
        {
            // A 1 m square slab 2 cm thick, as many upper floors are; the edge default is 15 cm.
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.localScale = new Vector3(1f, 0.02f, 1f);
            var slab = box.GetComponent<BoxCollider>();
            Object.DestroyImmediate(slab);
            var mesh = box.AddComponent<MeshCollider>();
            mesh.sharedMesh = box.GetComponent<MeshFilter>().sharedMesh;
            // On the top face (mesh space y = 0.5). metresPerUnit 1, so offsets come back in world metres.
            Vector3 top = new Vector3(0.2f, 0.5f, 0.2f);
            Vector3 down = CastleFractureBake.StoneEdgeInward(mesh, box.transform.localToWorldMatrix, top, Vector3.down, 0.15f, 1f);
            Assert.That(down.y, Is.EqualTo(-0.01f).Within(0.002f), "down half the 2 cm");
            // The face's normal pointing the wrong way (as some floors are wound) still finds the slab below.
            Vector3 flipped = CastleFractureBake.StoneEdgeInward(mesh, box.transform.localToWorldMatrix, top, Vector3.up, 0.15f, 1f);
            Assert.That(flipped.y, Is.EqualTo(-0.01f).Within(0.002f), "a face wound the other way still goes into the slab");
            box.transform.localScale = Vector3.one;
            Physics.SyncTransforms();
            Vector3 thick = CastleFractureBake.StoneEdgeInward(mesh, box.transform.localToWorldMatrix, top, Vector3.down, 0.1f, 1f);
            Assert.AreEqual(-0.1f, thick.y, 1e-5f, "a slab thicker than twice the default keeps the default");
            box.transform.position = new Vector3(0f, -100f, 0f);
            Physics.SyncTransforms();
            // A single sheet: nothing either side, so a level edge goes below whichever way the face is wound.
            Vector3 sheet = CastleFractureBake.StoneEdgeInward(mesh, Matrix4x4.identity, Vector3.zero, Vector3.up, 0.1f, 1f);
            Object.DestroyImmediate(box);
            Assert.AreEqual(-0.1f, sheet.y, 1e-5f, "a single level sheet hides its edge below");
        }
    }
}

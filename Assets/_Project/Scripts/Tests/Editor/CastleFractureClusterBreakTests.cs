using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Plunderspell.Fracture;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>A beam through a baked castle wall breaks whole clusters, the same ones every time, and the collider keeps
    /// exactly the front triangles of the clusters left standing (#491).</summary>
    public class CastleFractureClusterBreakTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned) Object.DestroyImmediate(go);
            foreach (FractureEffect fx in Object.FindObjectsByType<FractureEffect>(FindObjectsSortMode.None)) Object.DestroyImmediate(fx.gameObject);
            _spawned.Clear();
        }

        [Test]
        public void LineHitBreaksWholeClustersTheSameWayTwice()
        {
            string prefabPath = FindClusteredWallPrefab();
            Assert.IsNotNull(prefabPath, "no castle part prefab with a clustered Part_Wall; run Tools/Plunderspell/Castle/Bake Castle Room Fracture");

            List<int> first = HitThroughMiddle(prefabPath, out FracturableObject wall);
            FractureGraphData data = wall.bakedGraphData;
            Assert.That(first.Count, Is.GreaterThan(0), "the beam broke nothing");
            Assert.That(first.Count, Is.LessThan(data.clusterTriangles.Length), "the beam broke the whole wall");

            int expectedFront = 0;
            for (int t = 0; t < data.triangleCluster.Length; t++)
                if (data.triangleKind[t] == 0 && !wall.IsClusterBroken(data.triangleCluster[t])) expectedFront++;
            Mesh collider = wall.GetComponent<MeshCollider>().sharedMesh;
            Assert.AreEqual(expectedFront, collider.triangles.Length / 3, "the collider is not the standing clusters' front triangles");

            List<int> second = HitThroughMiddle(prefabPath, out _);
            CollectionAssert.AreEqual(first, second, "the same hit broke different clusters");
        }

        private List<int> HitThroughMiddle(string prefabPath, out FracturableObject wall)
        {
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            _spawned.Add(root);
            wall = root.transform.Find("Part_Wall").GetComponent<FracturableObject>();
            typeof(FracturableObject).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wall, null);

            // A room's walls surround empty floor, so aim at the middle cluster itself, along a level line as the beam comes.
            FractureGraphData data = wall.bakedGraphData;
            Vector3 target = wall.transform.TransformPoint(data.clusterCentroid[data.clusterCentroid.Length / 2]);
            wall.HitAlongLine(target - Vector3.right * 5f, target + Vector3.right * 5f, 0.75f, 5f);

            var broken = new List<int>();
            for (int c = 0; c < wall.bakedGraphData.clusterTriangles.Length; c++) if (wall.IsClusterBroken(c)) broken.Add(c);
            return broken;
        }

        private static string FindClusteredWallPrefab()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Transform part = AssetDatabase.LoadAssetAtPath<GameObject>(path).transform.Find("Part_Wall");
                if (part == null || !part.TryGetComponent(out FracturableObject fo) || fo.bakedGraphData == null) continue;
                FractureGraphData d = fo.bakedGraphData;
                if (d.clusterTriangles != null && d.clusterTriangles.Length >= 8 && d.triangleKind != null && d.triangleKind.Length == d.triangleCluster.Length)
                    return path;
            }
            return null;
        }
    }
}

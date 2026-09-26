using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Loot;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Guards how a carried item sits in the hand: upright, held at its grip point (or its mesh
    /// centre), never by its base. See docs/plans/staging-followups-2026-09-24.md, part B.
    /// </summary>
    public class LootGripTests
    {
        private static readonly string[] k_EraFolders =
        {
            "Assets/_Project/Prefabs/Loot/BronzeAge", "Assets/_Project/Prefabs/Loot/HighMedieval",
            "Assets/_Project/Prefabs/Loot/LateMedieval", "Assets/_Project/Prefabs/Loot/AgeOfPowder",
        };

        private static IEnumerable<string> PrefabPaths(params string[] folders)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", folders))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        private static Bounds MeshBounds(GameObject root)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (MeshRenderer meshRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!any)
                    bounds = meshRenderer.bounds;
                else
                    bounds.Encapsulate(meshRenderer.bounds);
                any = true;
            }
            return bounds;
        }

        [Test]
        public void EveryForgedItemHasAGripPointOnItsMesh()
        {
            int checkedCount = 0;
            foreach (string path in PrefabPaths(k_EraFolders))
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                try
                {
                    Transform grip = instance.GetComponent<LootPickup>().GripPoint;
                    Assert.IsNotNull(grip, $"{path} has no grip point; re-run Forge Era Content.");
                    Bounds bounds = MeshBounds(instance);
                    bounds.Expand(0.01f);
                    Assert.IsTrue(bounds.Contains(grip.position),
                        $"{path}: grip {grip.position} is outside the mesh bounds {bounds}.");
                    checkedCount++;
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
            Assert.AreEqual(20, checkedCount, "Expected the 20 art-bible plunder items.");
        }

        [Test]
        public void HeldItemIsUprightWithItsGripOnTheSocket()
        {
            var socket = new GameObject("TestHandSocket").transform;
            socket.SetPositionAndRotation(new Vector3(1f, 1.4f, -2f), Quaternion.Euler(10f, 70f, -5f));
            try
            {
                foreach (string path in PrefabPaths("Assets/_Project/Prefabs/Loot"))
                {
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                    try
                    {
                        var pickup = instance.GetComponent<LootPickup>();
                        if (pickup == null)
                            continue;
                        Quaternion upright = instance.transform.rotation;
                        pickup.CaptureUprightRotation();
                        Transform grip = pickup.GripPoint;
                        Vector3 centreBefore = instance.transform.InverseTransformPoint(MeshBounds(instance).center);

                        pickup.AttachToSocket(socket);

                        Assert.That(Quaternion.Angle(instance.transform.rotation, socket.rotation * upright), Is.LessThan(0.1f),
                            $"{path} is not held the way up it spawned.");
                        Vector3 held = grip != null ? grip.position : instance.transform.TransformPoint(centreBefore);
                        Assert.That(Vector3.Distance(held, socket.position), Is.LessThan(0.002f),
                            $"{path}: the {(grip != null ? "grip point" : "mesh centre")} is {Vector3.Distance(held, socket.position):F3} m from the hand.");
                    }
                    finally
                    {
                        Object.DestroyImmediate(instance);
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(socket.gameObject);
            }
        }
    }
}

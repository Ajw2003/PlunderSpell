using System.Collections.Generic;
using NUnit.Framework;
using Plunderspell.Loot;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.Tests.Editor
{
    /// <summary>
    /// Guards that every loot piece and weapon has an authored grip point on its mesh — the point
    /// <see cref="Item"/> (the raid's carry) hangs it from. See
    /// docs/plans/staging-followups-2026-09-24.md, part B.
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
                    Transform grip = instance.GetComponent<Item>().GripPoint;
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

        /// <summary>#134: the weapons and the five original loot pieces get authored grips too, on
        /// their mesh (the point <see cref="Item"/> hangs them from while held).</summary>
        [Test]
        public void EveryWeaponAndOriginalLootPieceHasAGripPointOnItsMesh()
        {
            var paths = new List<string>(PrefabPaths("Assets/_Project/Prefabs/Weapons"));
            foreach (string file in System.IO.Directory.GetFiles("Assets/_Project/Prefabs/Loot", "*.prefab"))
                paths.Add(file.Replace('\\', '/'));
            Assert.AreEqual(15, paths.Count, "Expected 10 weapons and 5 original loot pieces.");

            foreach (string path in paths)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                try
                {
                    var item = instance.GetComponent<Item>();
                    Assert.IsNotNull(item, $"{path} has no Item.");
                    Transform grip = item.GripPoint;
                    Assert.IsNotNull(grip, $"{path} has no grip point on its Item.");

                    Bounds bounds = MeshBounds(instance);
                    bounds.Expand(0.01f);
                    Assert.IsTrue(bounds.Contains(grip.position),
                        $"{path}: grip {grip.position} is outside the mesh bounds {bounds}.");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }
    }
}

using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using RogueAi.Castle;
using RogueAi.EditorTools;
using UnityEditor;
using UnityEngine;

namespace RogueAi.Tests.Editor
{
    /// <summary>
    /// Measures the art that actually shipped against the standard in docs/systems/scale.md, so the
    /// doc's invariants are checkable rather than a matter of opinion. Issue
    /// <see href="https://github.com/Ajw2003/PlunderSpell/issues/6"/>.
    ///
    /// These read real prefab bounds rather than restating <c>ZONE_HEIGHT</c> from
    /// <c>Tools/AssetPipeline/castle_builders.py</c>: a copy of that table in C# would pass while
    /// the meshes said something else, which is the failure mode this exists to catch.
    /// </summary>
    public class ScaleInvariantTests
    {
        private const string k_RegistryPath = "Assets/_Project/Data/Castle/CastleRoomRegistry.asset";
        private const string k_EnemyPrefabDirectory = "Assets/_Project/Prefabs/Enemies";

        /// <summary>The standard human, from docs/systems/scale.md, "The standard".</summary>
        private const float k_StandardHumanHeight = 1.80f;

        /// <summary>Floor slab thickness; a module's bounds include it, the clear height does not.</summary>
        private const float k_FloorSlabHeight = 0.30f;

        /// <summary>Authoring tolerance. The pipeline grows stacked boxes slightly to stop z-fighting.</summary>
        private const float k_Tolerance = 0.05f;

        /// <summary>How far an enemy's lowest vertex may sit from its own origin before it reads as
        /// floating or sunk in play. Owned by the forge so the repair tool and this test agree.</summary>
        private const float k_FootTolerance = EnemyPrefabForge.FootTolerance;

        [Test]
        public void Test_ThePlayerCapsuleIsTheStandardHuman()
        {
            Assert.AreEqual(k_StandardHumanHeight, CastleSpawnResolver.PlayerHeight, k_Tolerance,
                "The spawn probe must size itself to the documented standard human.");
            Assert.AreEqual(0.40f, CastleSpawnResolver.PlayerRadius, k_Tolerance);
        }

        /// <summary>
        /// The invariant that issue 6 is really about: a room a player walks into has to have room
        /// above their head, in every zone, measured off the mesh rather than off the plan.
        /// </summary>
        [Test]
        public void Test_EveryRoomModuleClearsAStandardHuman()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(k_RegistryPath);
            Assert.IsNotNull(registry, $"No room registry at {k_RegistryPath}.");

            var failures = new StringBuilder();
            int measured = 0;

            foreach (CastleRoomModuleData module in registry.Modules)
            {
                if (module == null || module.Prefab == null)
                {
                    continue;
                }

                float clearHeight = ClearHeightOf(module.Prefab);
                measured++;

                if (clearHeight < k_StandardHumanHeight)
                {
                    failures.AppendLine(
                        $"  {module.RoomId} ({module.Zone}): {clearHeight:F2} m clear, " +
                        $"under the {k_StandardHumanHeight:F2} m standard human.");
                }
            }

            Assert.Greater(measured, 0, "The registry has no prefabs, so this asserted nothing.");
            Assert.IsEmpty(failures.ToString(),
                $"Rooms a player cannot stand up in:\n{failures}");
        }

        /// <summary>
        /// No enemy may be taller than the shortest room it is posted to — otherwise it spawns
        /// clipping through the ceiling of a room it is supposed to fight in.
        /// </summary>
        [Test]
        public void Test_NoEnemyIsTallerThanTheRoomsItIsPostedTo()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CastleRoomRegistry>(k_RegistryPath);
            Assert.IsNotNull(registry, $"No room registry at {k_RegistryPath}.");

            Dictionary<CastleZone, float> shortestByZone = ShortestClearHeightPerZone(registry);
            var failures = new StringBuilder();
            int measured = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { k_EnemyPrefabDirectory }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    continue;
                }

                float height = RenderedHeightOf(prefab);
                measured++;

                foreach (KeyValuePair<CastleZone, float> zone in shortestByZone)
                {
                    // Every zone is checked, not just the posted ones: the roster maps enemy to zone
                    // in a ScriptableObject the registry knows nothing about, so the cheap honest
                    // assertion is against the tallest thing any room must hold.
                    if (height > zone.Value)
                    {
                        failures.AppendLine(
                            $"  {prefab.name}: {height:F2} m tall, over {zone.Key}'s " +
                            $"{zone.Value:F2} m clear height.");
                        break;
                    }
                }
            }

            Assert.Greater(measured, 0, "No enemy prefabs found, so this asserted nothing.");
            Assert.IsEmpty(failures.ToString(),
                $"Enemies taller than a room they could stand in:\n{failures}");
        }

        /// <summary>
        /// An enemy is spawned by putting its origin on the floor, so a model whose lowest vertex is
        /// not at its own origin hovers above the ground or sinks into it. Measured on the baked
        /// vertices: <c>Renderer.bounds</c> on a skinned mesh is padded and made GildedColossus and
        /// VaultWarden read as sunk while they stand exactly on the floor (issue 94).
        /// </summary>
        [Test]
        public void Test_EveryEnemyStandsOnItsOwnOrigin()
        {
            var failures = new StringBuilder();
            int measured = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { k_EnemyPrefabDirectory }))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                {
                    continue;
                }

                float lowest = LowestRenderedPointOf(prefab);
                measured++;

                if (Mathf.Abs(lowest) > k_FootTolerance)
                {
                    failures.AppendLine($"  {prefab.name}: lowest point at {lowest:F2} m, " +
                                        $"so it {(lowest > 0f ? "floats" : "sinks")}.");
                }
            }

            Assert.Greater(measured, 0, "No enemy prefabs found, so this asserted nothing.");
            Assert.IsEmpty(failures.ToString(), $"Enemies not standing on the floor:\n{failures}");
        }

        private static Dictionary<CastleZone, float> ShortestClearHeightPerZone(
            CastleRoomRegistry registry)
        {
            var shortest = new Dictionary<CastleZone, float>();

            foreach (CastleRoomModuleData module in registry.Modules)
            {
                if (module == null || module.Prefab == null)
                {
                    continue;
                }

                float clearHeight = ClearHeightOf(module.Prefab);
                if (!shortest.TryGetValue(module.Zone, out float current) || clearHeight < current)
                {
                    shortest[module.Zone] = clearHeight;
                }
            }

            return shortest;
        }

        /// <summary>Walkable height inside a module: its total bounds less the floor slab.</summary>
        private static float ClearHeightOf(GameObject prefab)
        {
            return Mathf.Max(0f, RenderedHeightOf(prefab) - k_FloorSlabHeight);
        }

        /// <summary>Height of everything a prefab renders, in the prefab root's own space.</summary>
        private static float RenderedHeightOf(GameObject prefab)
        {
            (float lowest, float highest) = VerticalExtentOf(prefab);
            return highest > lowest ? highest - lowest : 0f;
        }

        /// <summary>
        /// The lowest point a prefab draws, relative to its own origin, measured on the geometry
        /// itself. Unlike <see cref="VerticalExtentOf"/> this does not read <c>Renderer.bounds</c>,
        /// which for a rigged enemy is a padded box that does not track its feet.
        /// </summary>
        private static float LowestRenderedPointOf(GameObject prefab)
        {
            GameObject instance = Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            try
            {
                return PrefabGeometry.LowestY(instance);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        /// <summary>
        /// Lowest and highest rendered Y, measured by instantiating the prefab at the origin and
        /// reading real world-space renderer bounds.
        ///
        /// Prefab *assets* do not have meaningful world matrices, and rigged enemies render through
        /// <c>SkinnedMeshRenderer</c>, which has no <c>MeshFilter</c> — so walking the asset's
        /// hierarchy by hand measured some prefabs as zero-height and made these tests pass without
        /// asserting anything. Instantiating measures what the game actually renders.
        /// </summary>
        private static (float Lowest, float Highest) VerticalExtentOf(GameObject prefab)
        {
            GameObject instance = Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            try
            {
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
                float lowest = float.MaxValue;
                float highest = float.MinValue;

                foreach (Renderer renderer in renderers)
                {
                    if (!renderer.enabled && renderer is ParticleSystemRenderer)
                    {
                        continue;
                    }

                    Bounds bounds = renderer.bounds;
                    lowest = Mathf.Min(lowest, bounds.min.y);
                    highest = Mathf.Max(highest, bounds.max.y);
                }

                return lowest == float.MaxValue ? (0f, 0f) : (lowest, highest);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}

#pragma warning disable CS0618 // This tool exists to read the obsolete CastleGuard off the old prefabs.
using System.Text;
using Plunderspell.Guards;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Swaps the legacy <c>CastleGuard</c> for the fresh <see cref="Guard"/> on every guard prefab, in place
    /// (#214). Saving over the same asset keeps each prefab's GUID, so the NetworkPrefabs list and the
    /// roster still point at it. The tuning is read from the old component's serialized fields, so every
    /// hand edit made since the forges ran is carried over rather than rebuilt from the specs.
    /// Safe to run again: a prefab with no CastleGuard is skipped.
    /// </summary>
    public static class GuardPrefabSwapTool
    {
        private const string PrefabRoot = "Assets/_Project/Prefabs";

        [MenuItem("Tools/Plunderspell/Swap Guard Prefabs To Fresh Guard")]
        public static string SwapAll()
        {
            var report = new StringBuilder();
            int swapped = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (SwapOne(path, report))
                    swapped++;
            }

            AssetDatabase.SaveAssets();
            report.AppendLine($"swapped {swapped} prefab(s)");
            Debug.Log($"Plunderspell: guard prefab swap.\n{report}");
            return report.ToString();
        }

        private static bool SwapOne(string path, StringBuilder report)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CastleGuard legacy = root.GetComponent<CastleGuard>();
                if (legacy == null)
                    return false;

                GuardPrefabSetup setup = ReadLegacyTuning(legacy, root.GetComponent<NavMeshAgent>());
                float meleeReach = ReadFloat(legacy, "_attackRange");
                float damage = ReadFloat(legacy, "_attackDamage");
                float cooldown = ReadFloat(legacy, "_attackCooldown");
                float projectileSpeed = ReadFloat(legacy, "_projectileSpeed");
                float fieldOfView = ReadFloat(legacy, "_fieldOfView");

                RemoveLegacyParts(root);

                Guard guard = GuardPrefabParts.Add(root, setup);
                CarryAttackTuning(guard, meleeReach, damage, cooldown, projectileSpeed, fieldOfView);

                PrefabUtility.SaveAsPrefabAsset(root, path);
                report.AppendLine($"{root.name,-18} swapped ({path})");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GuardPrefabSetup ReadLegacyTuning(CastleGuard legacy, NavMeshAgent agent)
        {
            var serialized = new SerializedObject(legacy);
            return new GuardPrefabSetup
            {
                SightRange = ReadFloat(legacy, "_sightRange"),
                PatrolSpeed = ReadFloat(legacy, "_patrolSpeed"),
                ChaseSpeed = ReadFloat(legacy, "_chaseSpeed"),
                MaxHealth = ReadFloat(legacy, "_maxHealth"),
                EyeHeight = ReadFloat(legacy, "_eyeHeight"),
                // The agent's shape is what the old body swept the castle with; the fresh guard sweeps the same.
                BodyRadius = agent != null ? agent.radius : 0.4f,
                BodyHeight = agent != null ? agent.height : 1.8f,
                ProjectilePrefab = serialized.FindProperty("_projectilePrefab").objectReferenceValue as GameObject
            };
        }

        private static float ReadFloat(CastleGuard legacy, string field)
        {
            return new SerializedObject(legacy).FindProperty(field).floatValue;
        }

        // DestroyImmediate is how a component is removed from loaded prefab contents. The Rigidbody is
        // normally added at runtime, so it is usually absent here; it is removed in case a prefab has one.
        private static void RemoveLegacyParts(GameObject root)
        {
            DestroyIfPresent(root.GetComponent<NavMeshAgent>());
            DestroyIfPresent(root.GetComponent<Rigidbody>());
            DestroyIfPresent(root.GetComponent<CastleGuard>());
        }

        private static void DestroyIfPresent(Object component)
        {
            if (component != null)
                Object.DestroyImmediate(component, true);
        }

        private static void CarryAttackTuning(Guard guard, float meleeReach, float damage, float cooldown,
            float projectileSpeed, float fieldOfView)
        {
            var serialized = new SerializedObject(guard);
            serialized.FindProperty("_tuning.MeleeReach").floatValue = meleeReach;
            serialized.FindProperty("_tuning.AttackDamage").floatValue = damage;
            serialized.FindProperty("_tuning.AttackCooldownSeconds").floatValue = cooldown;
            serialized.FindProperty("_tuning.ProjectileSpeed").floatValue = projectileSpeed;
            serialized.FindProperty("_tuning.FieldOfView").floatValue = fieldOfView;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

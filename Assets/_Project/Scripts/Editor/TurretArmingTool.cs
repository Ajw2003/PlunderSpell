using Plunderspell.Guards;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// Hands the HexTurret a projectile, which is what makes it a turret rather than a guard that
    /// cannot walk. Deliberately separate from <c>EnemyPrefabForge</c>: the forge rebuilds all ten
    /// enemy prefabs and would discard the hand-tuning they already carry.
    ///
    /// See docs/4-systems/raid.md, "Guards that can actually hurt you".
    /// </summary>
    public static class TurretArmingTool
    {
        private const string k_TurretPath = "Assets/_Project/Prefabs/Enemies/HexTurret.prefab";
        private const string k_BoltPath = "Assets/_Project/Prefabs/Projectiles/Bolt.prefab";

        [MenuItem("Tools/Plunderspell/Arm The HexTurret")]
        public static void ArmTheHexTurret()
        {
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(k_BoltPath);
            if (bolt == null)
            {
                Debug.LogError($"[Turret] No projectile at {k_BoltPath}. Run Forge Projectile Prefab first.");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(k_TurretPath);
            try
            {
                if (!root.TryGetComponent(out CastleGuard guard))
                {
                    Debug.LogError($"[Turret] {k_TurretPath} has no CastleGuard.");
                    return;
                }

                var serialized = new SerializedObject(guard);
                serialized.FindProperty("_projectilePrefab").objectReferenceValue = bolt;
                serialized.FindProperty("_attackCooldown").floatValue = 2.2f;
                serialized.FindProperty("_attackDamage").floatValue = 14f;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, k_TurretPath);
                Debug.Log($"[Turret] Armed {k_TurretPath} with {k_BoltPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
        }
    }
}

using Plunderspell.Guards;
using UnityEditor;
using UnityEngine;

namespace Plunderspell.EditorTools
{
    /// <summary>The numbers a guard prefab is built with. Anything left at its default keeps the fresh guard's own default.</summary>
    public sealed class GuardPrefabSetup
    {
        public float SightRange = 14f;
        public float PatrolSpeed = 2f;
        public float ChaseSpeed = 4.5f;
        public float MaxHealth = 100f;
        public float EyeHeight = 1.6f;
        public float BodyRadius = 0.4f;
        public float BodyHeight = 1.8f;

        /// <summary>Set for archers, mages and turrets; null means a melee guard.</summary>
        public GameObject ProjectilePrefab;

        /// <summary>Negative keeps the default.</summary>
        public float AttackCooldownSeconds = -1f;
    }

    /// <summary>
    /// The one place that puts the fresh <see cref="Guard"/> on a prefab, so every forge (EnemyPrefabForge,
    /// ArtBibleEnemyForge, EraContentForge) and the in-place swap give a guard the same parts and a
    /// rebuild of any of them cannot bring the legacy guard back (#214). A guard has no NavMeshAgent and no
    /// Rigidbody: the director's navigation service moves it, so neither is added here.
    /// </summary>
    public static class GuardPrefabParts
    {
        /// <summary>Layer 1 is Default, which the castle geometry is on. Without it the sight check never
        /// hits a wall and guards see through the castle.</summary>
        private const int CastleGeometryLayerMask = 1;

        /// <summary>Adds a <see cref="Guard"/> (and the parts it requires) to <paramref name="root"/> and tunes it.</summary>
        public static Guard Add(GameObject root, GuardPrefabSetup setup)
        {
            var guard = root.AddComponent<Guard>();
            Apply(guard, setup);
            return guard;
        }

        /// <summary>Writes the setup into the guard's serialized tuning, without widening the runtime API for a tool.</summary>
        public static void Apply(Guard guard, GuardPrefabSetup setup)
        {
            var serialized = new SerializedObject(guard);
            SetFloat(serialized, "SightRange", setup.SightRange);
            SetFloat(serialized, "PatrolSpeed", setup.PatrolSpeed);
            SetFloat(serialized, "ChaseSpeed", setup.ChaseSpeed);
            SetFloat(serialized, "MaxHealth", setup.MaxHealth);
            SetFloat(serialized, "EyeHeight", setup.EyeHeight);
            SetFloat(serialized, "BodyRadius", setup.BodyRadius);
            SetFloat(serialized, "BodyHeight", setup.BodyHeight);
            serialized.FindProperty("_tuning.GeometryLayers").intValue = CastleGeometryLayerMask;
            serialized.FindProperty("_tuning.ProjectilePrefab").objectReferenceValue = setup.ProjectilePrefab;
            if (setup.AttackCooldownSeconds >= 0f)
                SetFloat(serialized, "AttackCooldownSeconds", setup.AttackCooldownSeconds);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(SerializedObject serialized, string tuningField, float value)
        {
            serialized.FindProperty("_tuning." + tuningField).floatValue = value;
        }
    }
}

using System;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Every number a guard is tuned by, in one serializable block so the prefab shows them in one place
    /// and the states read them from the guard instead of holding their own copies.
    /// </summary>
    [Serializable]
    public sealed class GuardTuning
    {
        [Header("Senses")]
        [Tooltip("How far this guard can see, in metres. It does not grow with the alarm (re-add: #229).")]
        public float SightRange = 14f;

        [Tooltip("Field of view in degrees.")]
        public float FieldOfView = 110f;

        [Tooltip("Eye height above the guard pivot, in metres.")]
        public float EyeHeight = 1.6f;

        [Tooltip("Height above a player pivot the guard looks at, in metres.")]
        public float TargetAimHeight = 1.0f;

        [Tooltip("Layers that block line of sight. Leave players out.")]
        public LayerMask GeometryLayers;

        [Header("Movement")]
        public float PatrolSpeed = 2.0f;
        public float ChaseSpeed = 4.5f;

        [Tooltip("Body shape the navigation service sweeps with.")]
        public float BodyRadius = 0.4f;
        public float BodyHeight = 1.8f;

        [Header("Combat")]
        [Tooltip("Damage per hit. Scaled by the lobby size at spawn.")]
        public float AttackDamage = 12f;

        [Header("Health")]
        public float MaxHealth = 100f;
    }
}

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

        [Header("Patrol")]
        [Tooltip("Points a patrol round walks. At least 3 so the guard never just paces between two.")]
        public int PatrolPointCount = 3;

        public float PatrolMinimumRadius = 3f;
        public float PatrolMaximumRadius = 8f;

        [Tooltip("Points closer together than this count as the same point.")]
        public float PatrolPointSpacing = 1.5f;

        [Tooltip("Standing time at each point, in seconds.")]
        public float PatrolPauseSeconds = 1f;

        [Tooltip("Random tries to find one reachable point before giving up until the next pause ends.")]
        public int PatrolPickAttempts = 16;

        [Header("Investigate")]
        [Tooltip("Walking speed to a noise or sighting, between a patrol walk and a chase.")]
        public float InvestigateSpeed = 3.2f;

        [Tooltip("How long the guard stands and looks around at the spot, in seconds.")]
        public float InvestigateLookSeconds = 3f;

        [Header("Chase")]
        [Tooltip("How often a chasing guard may re-plan toward a moving player, in seconds. Keeps the route service from re-planning every frame.")]
        public float ChaseRetargetSeconds = 0.25f;

        [Tooltip("The player must have moved this far from the planned destination before the guard re-plans, in metres.")]
        public float ChaseRetargetDistance = 0.75f;

        [Tooltip("How long the guard keeps running to the last seen spot before it gives up and investigates, in seconds. Stops a player flickering at the edge of the view from dropping the chase.")]
        public float ChaseLoseSightSeconds = 0.75f;

        [Tooltip("A melee guard hands over to combat within this distance of the player, in metres (legacy attack range).")]
        public float MeleeReach = 2f;

        [Tooltip("A ranged guard hands over to combat within this distance of the player, in metres. It shoots from further out while it runs.")]
        public float RangedEngageRange = 8f;

        [Header("Ranged attack")]
        [Tooltip("Set on archers and mages. Empty means a melee guard.")]
        public GameObject ProjectilePrefab;

        [Tooltip("Metres per second the shot leaves at (legacy 18).")]
        public float ProjectileSpeed = 18f;

        [Tooltip("Seconds between shots (legacy 1.4).")]
        public float AttackCooldownSeconds = 1.4f;

        [Header("Movement body")]
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

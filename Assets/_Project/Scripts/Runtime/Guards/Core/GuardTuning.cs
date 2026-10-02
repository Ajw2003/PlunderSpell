using System;
using Plunderspell.Alarm;
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

        [Tooltip("Radius of the sphere swept along a shot to look for a teammate in the way, in metres (#210).")]
        public float ShotClearanceRadius = 0.3f;

        [Header("Movement body")]
        [Tooltip("Body shape the navigation service sweeps with.")]
        public float BodyRadius = 0.4f;
        public float BodyHeight = 1.8f;

        [Header("Combat")]
        [Tooltip("Damage per hit. Scaled by the lobby size at spawn.")]
        public float AttackDamage = 12f;

        [Tooltip("Combat holds while the player is within the reach plus this, in metres, so a player stepping back does not flicker the guard between Chase and Combat (#210).")]
        public float CombatMargin = 2.5f;

        [Tooltip("A guard waiting its turn stands this far outside melee reach, or inside ranged range, in metres.")]
        public float CombatRingPadding = 1f;

        [Tooltip("After a strike or a shot the guard keeps its turn this long, in seconds, so guards' attacks do not land in the same frame. The legacy swing had no windup, so this is new.")]
        public float AttackRecoverySeconds = 0.5f;

        [Tooltip("How fast a fighting guard turns to face the player, in degrees per second.")]
        public float TurnDegreesPerSecond = 360f;

        [Tooltip("How often a guard without a turn asks again, in seconds.")]
        public float TurnRequestSeconds = 0.2f;

        [Header("Stunned and slept")]
        [Tooltip("When a stun or sleep ends with nothing heard, a guard goes to look around where it stands if the castle is at least this alert. Below it, it goes back to its round.")]
        public AlarmState InvestigateAfterRecoveryFrom = AlarmState.Roused;

        [Tooltip("A levitated guard counts as landed once its pivot is this close to the floor, in metres.")]
        public float LandingTolerance = 0.15f;

        [Header("Health")]
        public float MaxHealth = 100f;
    }
}

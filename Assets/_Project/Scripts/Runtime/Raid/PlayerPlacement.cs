using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>Stands a player at a floor point, facing its forward: the Lair spawns and the Lair-Market doorways.</summary>
    public static class PlayerPlacement
    {
        /// <summary>The body's centre stands this far above the floor point.</summary>
        private const float BodyHeightAboveFloor = 1.0f;

        public static void StandAt(PlayerStateMachine player, Transform floorPoint)
        {
            Vector3 position = floorPoint.position + Vector3.up * BodyHeightAboveFloor;
            player.FaceYaw(floorPoint.eulerAngles.y);
            // Through the rigidbody as well as the transform: an interpolated body writes its old
            // position back over a transform-only move on the next physics step.
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
            }
            player.transform.position = position;
            Debug.Log($"[Lair] Placed {player.name} at {position} ({floorPoint.parent?.name}/{floorPoint.name}).");
        }
    }
}

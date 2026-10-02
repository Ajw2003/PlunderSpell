using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Turns a guard to face the way it is walking, a little each tick at
    /// <see cref="GuardNavigationTuning.TurnDegreesPerSecond"/>, so a patrolling or chasing guard looks where
    /// it goes (its sight cone follows its facing). Only the yaw is written, never the position.
    ///
    /// A move with <see cref="MoveReason.Combat"/> is left alone: a fighting guard keeps its eyes on the
    /// player while it walks round to its place, and that facing belongs to the Combat state.
    /// </summary>
    public sealed class GuardMoverFacing
    {
        private const float MinimumStepSquared = 0.0000001f;

        private readonly GuardNavigationTuning _tuning;

        public GuardMoverFacing(GuardNavigationTuning tuning)
        {
            _tuning = tuning;
        }

        /// <summary>Turns the guard toward <paramref name="pathStep"/>, the direction the route wants it to go this tick.</summary>
        public void Turn(GuardMover mover, Vector3 pathStep, float deltaTime)
        {
            if (mover.Reason == MoveReason.Combat)
                return;

            pathStep.y = 0f;
            if (pathStep.sqrMagnitude < MinimumStepSquared)
                return;

            Quaternion wanted = Quaternion.LookRotation(pathStep, Vector3.up);
            mover.Body.rotation = Quaternion.RotateTowards(mover.Body.rotation, wanted, _tuning.TurnDegreesPerSecond * deltaTime);
        }
    }
}

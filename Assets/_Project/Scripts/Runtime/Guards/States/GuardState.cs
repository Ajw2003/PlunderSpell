using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A guard state. Adds to the generic state one thing the guard needs from every state: which
    /// replicated <see cref="GuardAlertState"/> clients, audio and the HUD should see while it is
    /// active. A state decides its own exits by returning the next state from Tick, so the guard
    /// itself holds no transition logic.
    /// </summary>
    public abstract class GuardState : State<Guard>
    {
        protected GuardState(Guard guard) : base(guard)
        {
        }

        /// <summary>The value written to the replicated state while this state is active.</summary>
        public abstract GuardAlertState AlertState { get; }

        /// <summary>
        /// Turns the guard (yaw only) toward <paramref name="target"/>. The position stays with the navigation
        /// service. Without facing, a guard that walked round to its place would have the player behind it and
        /// lose sight of them.
        /// </summary>
        protected void FaceToward(Transform target, float deltaTime)
        {
            Vector3 toTarget = target.position - Context.transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                return;

            Quaternion facing = Quaternion.LookRotation(toTarget, Vector3.up);
            Context.transform.rotation = Quaternion.RotateTowards(Context.transform.rotation, facing,
                Context.Tuning.TurnDegreesPerSecond * deltaTime);
        }
    }
}

using UnityEngine;

// Base class for all player state machine states - walking, jumping, in an inventory UI, etc.
namespace StateMachine
{
    public class PlayerState : IState
    {
        protected PlayerStateMachine _stateMachine;

        public PlayerState(PlayerStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public virtual void Enter()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        // Movement input resolved against the camera and flattened onto the ground plane, so
        // looking up or down never pushes the player into or out of the floor. Returns a unit
        // vector, or zero when there is no input.
        protected Vector3 CameraRelativeInput()
        {
            Vector3 cameraForward = Vector3.ProjectOnPlane(_stateMachine.CameraTransform.forward, Vector3.up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_stateMachine.CameraTransform.right, Vector3.up).normalized;

            Vector3 moveDirection = (cameraForward * _stateMachine.MovementDirection.y)
                                    + (cameraRight * _stateMachine.MovementDirection.x);

            return moveDirection.normalized;
        }
    }
}

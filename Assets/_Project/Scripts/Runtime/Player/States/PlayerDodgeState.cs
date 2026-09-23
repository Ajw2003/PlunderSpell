using UnityEngine;

namespace StateMachine.States
{
    public class PlayerDodgeState : PlayerState
    {
        public PlayerDodgeState(PlayerStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            // Camera-relative like walking: the body no longer turns with the view (see
            // PlayerStateMachine.Look), so its own axes say nothing about where "forward" is.
            Vector3 dodgeDirection = CameraRelativeInput();
            _stateMachine._rb.AddForce(dodgeDirection * _stateMachine.DodgeForce, ForceMode.Impulse);
        }

        public override void FixedUpdate()
        {
            if (_stateMachine._rb.linearVelocity.magnitude < 1.0f)
            {
                Exit();
            }
        }

        public override void Exit()
        {
            if (_stateMachine.MovementDirection.sqrMagnitude > 0.1f)
            {
                _stateMachine.ChangeState(_stateMachine.WalkState);
            }
            else
            {
                _stateMachine.ChangeState(_stateMachine.IdleState);
            }
        }
    }
}

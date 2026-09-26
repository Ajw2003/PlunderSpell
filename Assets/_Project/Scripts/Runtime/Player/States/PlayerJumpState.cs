using StateMachine;
using UnityEngine;

public class PlayerJumpState : PlayerState
{
    private float _jumpTime;

    public PlayerJumpState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        _stateMachine._rb.AddForce(Vector3.up * _stateMachine.JumpForce, ForceMode.Impulse);
        _jumpTime = Time.time;
    }

    public override void FixedUpdate()
    {
        HandleAirSteering();

        // Small grace period before checking grounded, so we've actually left the ground.
        if (Time.time > _jumpTime + 0.2f && _stateMachine.IsGrounded)
        {
            Exit();
        }
    }

    private void HandleAirSteering()
    {
        Vector3 moveInput = CameraRelativeInput();
        if (moveInput.sqrMagnitude <= 0.01f) return;

        float steeringForce = _stateMachine.walkSpeed * _stateMachine.AirControl * 5f;
        _stateMachine._rb.AddForce(moveInput * steeringForce, ForceMode.Acceleration);

        ClampHorizontalVelocity();
    }

    private void ClampHorizontalVelocity()
    {
        Vector3 velocity = _stateMachine._rb.linearVelocity;
        Vector3 verticalVelocity = Vector3.up * velocity.y;
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        if (horizontalVelocity.magnitude > _stateMachine.walkSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * _stateMachine.walkSpeed;
            _stateMachine._rb.linearVelocity = horizontalVelocity + verticalVelocity;
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

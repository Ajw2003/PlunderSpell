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
        // A Saltus launch has already set the upward speed.
        if (!_stateMachine.TakeSpellLaunch())
            _stateMachine.Rb.AddForce(Vector3.up * _stateMachine.JumpForce, ForceMode.Impulse);
        _jumpTime = Time.time;
    }

    public override void FixedUpdate()
    {
        HandleAirSteering();

        // Small grace period before checking grounded, so we've actually left the ground.
        if (Time.time > _jumpTime + 0.2f && _stateMachine.IsGrounded)
        {
            Finish();
        }
    }

    private void HandleAirSteering()
    {
        Vector3 moveInput = CameraRelativeInput();
        if (moveInput.sqrMagnitude <= 0.01f) return;

        float steeringForce = _stateMachine.WalkSpeed * _stateMachine.AirControl * 5f;
        _stateMachine.Rb.AddForce(moveInput * steeringForce, ForceMode.Acceleration);

        ClampHorizontalVelocity();
    }

    private void ClampHorizontalVelocity()
    {
        Vector3 velocity = _stateMachine.Rb.linearVelocity;
        Vector3 verticalVelocity = Vector3.up * velocity.y;
        Vector3 horizontalVelocity = velocity - verticalVelocity;

        if (horizontalVelocity.magnitude > _stateMachine.WalkSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * _stateMachine.WalkSpeed;
            _stateMachine.Rb.linearVelocity = horizontalVelocity + verticalVelocity;
        }
    }

    // Hands control back to walk or idle. This used to be Exit, but ChangeState now runs Exit on the
    // state being left, so a transition inside Exit would recurse forever.
    private void Finish()
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

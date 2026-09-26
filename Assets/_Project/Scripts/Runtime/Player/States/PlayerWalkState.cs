using StateMachine;
using UnityEngine;

public class PlayerWalkState : PlayerState
{
    public PlayerWalkState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Update()
    {
        if (_stateMachine.CameraTransform == null)
        {
            Debug.LogWarning("CameraTransform is not assigned in PlayerStateMachine. Cannot apply camera-relative movement.");
            return;
        }

        Vector3 moveDirection = CameraRelativeInput();

        // Preserve the vertical component so this doesn't interfere with jumping/falling.
        float verticalSpeed = _stateMachine._rb.linearVelocity.y;

        _stateMachine._rb.linearVelocity = (moveDirection * _stateMachine.walkSpeed) + (Vector3.up * verticalSpeed);
    }

    public override void Exit()
    {
        // Move to idle state when movement stops - TODO, not carried from ThirdPerson.
    }
}

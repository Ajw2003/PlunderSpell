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

        // A heavy load slows you down: weight is felt in the legs, not only the arms.
        Item carried = ItemManager.Instance != null ? ItemManager.Instance.CarriedItem : null;
        float load = carried != null ? carried.CarrySpeedMultiplier : 1f;

        _stateMachine._rb.linearVelocity = (moveDirection * _stateMachine.walkSpeed * load) + (Vector3.up * verticalSpeed);
    }

    public override void Exit()
    {
        // Move to idle state when movement stops - TODO, not carried from ThirdPerson.
    }
}

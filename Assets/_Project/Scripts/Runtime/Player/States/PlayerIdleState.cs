using Code.Scripts.EventSystems;
using StateMachine;
using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine)
    {
    }

    public override void Enter()
    {
        EventManager.Instance?.Publish(new PlayerIdleEvent());
    }

    public override void FixedUpdate()
    {
        // Standing still means standing still. The ground snap holds the body's height and nothing else
        // reliably bleeds off sideways speed: without this, arriving here moving (the
        // end of a dodge, a scripted stop) coasted forever, off the edge of the map.
        Vector3 velocity = _stateMachine.Rb.linearVelocity;
        _stateMachine.Rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}

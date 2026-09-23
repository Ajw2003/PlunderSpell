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

    public override void Update()
    {
        // Standing still means standing still. The body hovers on its ground snap and never touches
        // the floor, so nothing else ever bleeds off speed: without this, arriving here moving (the
        // end of a dodge, a scripted stop) coasted forever, off the edge of the map.
        Vector3 velocity = _stateMachine._rb.linearVelocity;
        _stateMachine._rb.linearVelocity = new Vector3(0f, velocity.y, 0f);
    }
}

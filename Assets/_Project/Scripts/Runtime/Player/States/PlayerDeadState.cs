namespace StateMachine.States
{
    /// <summary>
    /// Down and out. The body stops; the raid, not this state, decides what happens next (a lost
    /// raid and the "You died" screen — see PlayerStateMachine.Die). It used to reload the scene,
    /// which wiped the raid with no explanation and left the game state on a menu nobody could see.
    /// </summary>
    public class PlayerDeadState : PlayerState
    {
        public PlayerDeadState(PlayerStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            if (_stateMachine._rb != null)
                _stateMachine._rb.linearVelocity = UnityEngine.Vector3.zero;
            _stateMachine.MovementDirection = UnityEngine.Vector2.zero;
        }
    }
}

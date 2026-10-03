namespace StateMachine.States
{
    public class PlayerAttackState : PlayerState
    {
        public PlayerAttackState(PlayerStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Exit()
        {
            // Nothing to undo. It used to switch to Idle here, which would now recurse since ChangeState runs Exit.
        }
    }
}

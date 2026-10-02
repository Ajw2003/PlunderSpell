namespace StateMachine.States
{
    /// <summary>Deprecated: kept as reference, not live.</summary>
    [System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
    public class MonsterDeadState : MonsterState
    {
        public MonsterDeadState(MonsterStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            _stateMachine.DestroySelf();
        }

        public override void Update()
        {
            // Nothing should happen once dead.
        }

        public override void Exit()
        {
        }
    }
}

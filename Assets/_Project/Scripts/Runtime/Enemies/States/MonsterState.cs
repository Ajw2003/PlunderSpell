using StateMachine;

/// <summary>Deprecated: kept as reference, not live.</summary>
[System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
public class MonsterState : IState
{
    protected MonsterStateMachine _stateMachine;

    public MonsterState(MonsterStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    public virtual void Enter()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void Exit()
    {
    }

    public virtual void FixedUpdate()
    {
    }
}

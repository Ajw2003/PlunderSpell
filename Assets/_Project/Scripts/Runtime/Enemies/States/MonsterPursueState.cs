namespace StateMachine.States
{
    /// <summary>Deprecated: kept as reference, not live.</summary>
    [System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
    public class MonsterPursueState : MonsterState
    {
        private float _repathTimer;

        public MonsterPursueState(MonsterStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            _repathTimer = 0f;
            _stateMachine.agent.enabled = true;
        }

        public override void Update()
        {
            if (_stateMachine.PlayerTarget != null)
            {
                float sqrDistance = (_stateMachine.transform.position - _stateMachine.PlayerTarget.position).sqrMagnitude;
                if (sqrDistance <= _stateMachine.AttackRange * _stateMachine.AttackRange)
                {
                    _stateMachine.ChangeState(_stateMachine.AttackState);
                    return;
                }
            }
            else
            {
                _stateMachine.ChangeState(_stateMachine.PatrolState);
                return;
            }

            // Throttle pathfinding and vision checks to improve performance.
            _repathTimer += UnityEngine.Time.deltaTime;
            if (_repathTimer < 0.2f) return;
            _repathTimer = 0f;

            if (!_stateMachine.CanSeePlayer())
            {
                _stateMachine.ChangeState(_stateMachine.PatrolState);
                return;
            }

            _stateMachine.MoveTo(_stateMachine.PlayerTarget.position);
        }

        public override void Exit()
        {
        }
    }
}

using UnityEngine;

namespace StateMachine.States
{
    /// <summary>Deprecated: kept as reference, not live.</summary>
    [System.Obsolete("Deprecated 2026-10-01: unused by any prefab, scene or code. See docs/reference/deprecated-code.md")]
    public class MonsterPatrolState : MonsterState
    {
        private float _visionTimer;

        public MonsterPatrolState(MonsterStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            if (_stateMachine.PatrolPoints.Count == 0)
            {
                _stateMachine.ChangeState(_stateMachine.IdleState);
                return;
            }
            _stateMachine.agent.enabled = true;
            _visionTimer = 0f;
            MoveToNextPoint();
        }

        public override void Update()
        {
            if (_stateMachine.HasReachedDestination())
            {
                _stateMachine.CurrentPatrolPointIndex = (_stateMachine.CurrentPatrolPointIndex + 1) % _stateMachine.PatrolPoints.Count;
                MoveToNextPoint();
            }

            _visionTimer += Time.deltaTime;
            if (_visionTimer < 0.2f) return;
            _visionTimer = 0f;

            if (_stateMachine.CanSeePlayer())
            {
                _stateMachine.ChangeState(_stateMachine.PursueState);
                return;
            }
        }

        private void MoveToNextPoint()
        {
            if (_stateMachine.PatrolPoints.Count == 0) return;
            Vector3 targetPoint = _stateMachine.PatrolPoints[_stateMachine.CurrentPatrolPointIndex];
            _stateMachine.MoveTo(targetPoint);
        }

        public override void Exit()
        {
        }
    }
}

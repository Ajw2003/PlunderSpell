using Interfaces;
using UnityEngine;

namespace StateMachine.States
{
    public class MonsterAttackState : MonsterState
    {
        private float _lastAttackTime;

        public MonsterAttackState(MonsterStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            _stateMachine.StopMoving();
            _lastAttackTime = -_stateMachine.AttackRate; // Allow an immediate first attack.
        }

        public override void Update()
        {
            if (_stateMachine.PlayerTarget == null)
            {
                _stateMachine.ChangeState(_stateMachine.PatrolState);
                return;
            }

            Vector3 lookPos = _stateMachine.PlayerTarget.position;
            lookPos.y = _stateMachine.transform.position.y;
            _stateMachine.transform.LookAt(lookPos);

            float distance = Vector3.Distance(_stateMachine.transform.position, _stateMachine.PlayerTarget.position);

            if (distance > _stateMachine.AttackRange)
            {
                _stateMachine.ChangeState(_stateMachine.PursueState);
                return;
            }

            if (Time.time >= _lastAttackTime + _stateMachine.AttackRate)
            {
                Attack();
                _lastAttackTime = Time.time;
            }
        }

        private void Attack()
        {
            // IHealth rather than PlayerStateMachine directly - keeps Enemies from needing a
            // compile-time reference to Player.
            if (_stateMachine.PlayerTarget.TryGetComponent(out IHealth targetHealth))
            {
                GameObject attacker = _stateMachine.gameObject;
                Damage.Apply(targetHealth, _stateMachine.AttackDamage, attacker, attacker,
                    Damage.PointOn(_stateMachine.PlayerTarget, attacker.transform.position + Vector3.up),
                    DamageKind.EnemyAttack);
            }
        }
    }
}

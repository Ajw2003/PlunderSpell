using Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace StateMachine.States
{
    public class MonsterPickedUpState : MonsterState
    {
        private NavMeshAgent _agent;
        private Rigidbody _rb;
        private Item _item;
        private float _escapeTimer;
        private float _damageTickTimer;

        public MonsterPickedUpState(MonsterStateMachine stateMachine) : base(stateMachine)
        {
            _agent = stateMachine.GetComponent<NavMeshAgent>();
            _rb = stateMachine.GetComponent<Rigidbody>();
            _item = stateMachine.GetComponent<Item>();
        }

        public override void Enter()
        {
            base.Enter();

            if (_item == null) _item = _stateMachine.GetComponent<Item>();

            // Stop movement instead of calling Deactivate() (which would change state to Idle).
            _stateMachine.StopMoving();

            if (_agent != null)
            {
                _agent.enabled = false;
            }

            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            _stateMachine.StartStruggling();
            _escapeTimer = Random.Range(_stateMachine.MinEscapeTime, _stateMachine.MaxEscapeTime);
        }

        public override void Exit()
        {
            base.Exit();
            // Stop the struggle timer if we exit for any other reason (like being thrown).
            _stateMachine.StopStruggling();
        }

        public override void Update()
        {
            HandleChokeDamage();
        }

        private void HandleChokeDamage()
        {
            // Only apply damage while actually being dragged/held by the player.
            if (_item == null || !_item.IsDragging) return;
            if (_stateMachine.PlayerTarget == null) return;
            if (!_stateMachine.PlayerTarget.TryGetComponent(out IChokeDamageSource chokeSource)) return;

            _damageTickTimer += Time.deltaTime;
            if (_damageTickTimer >= 1.0f)
            {
                GameObject choker = _stateMachine.PlayerTarget.gameObject;
                Damage.Apply(_stateMachine, chokeSource.ChokeDamage, choker, choker,
                    _stateMachine.transform.position + Vector3.up, DamageKind.Choke);
                _damageTickTimer = 0f;
                Debug.Log($"{_stateMachine.gameObject.name} taking choke damage: {chokeSource.ChokeDamage}");
            }
        }
    }
}

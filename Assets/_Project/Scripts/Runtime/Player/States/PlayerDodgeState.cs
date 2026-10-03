using UnityEngine;

namespace StateMachine.States
{
    public class PlayerDodgeState : PlayerState
    {
        private Vector3 _direction;
        private float _speed;
        private float _endsAt;

        public PlayerDodgeState(PlayerStateMachine stateMachine) : base(stateMachine)
        {
        }

        public override void Enter()
        {
            // Camera-relative like walking: the body no longer turns with the view (see
            // PlayerStateMachine.Look), so its own axes say nothing about where "forward" is.
            // Only Velox dodges now (the dodge key is gone), so a dash always goes somewhere.
            _direction = _stateMachine.TakeDashDirection(CameraRelativeInput(), out _speed, out float seconds);
            _endsAt = Time.time + seconds;
            Hold();
        }

        // The dash holds its speed for its whole length. It used to be one impulse and an exit once
        // the body was slower than 1 m/s, which the very first physics step always was, before the
        // impulse had been applied, so the dodge ended at once and went nowhere.
        public override void FixedUpdate()
        {
            if (Time.time >= _endsAt)
            {
                Finish();
                return;
            }
            Hold();
        }

        private void Hold()
        {
            float vertical = _stateMachine._rb.linearVelocity.y;
            _stateMachine._rb.linearVelocity = _direction * _speed + Vector3.up * vertical;
        }

        // Hands control back to walk or idle. This used to be Exit, but ChangeState now runs Exit on the
        // state being left, so a transition inside Exit would recurse forever.
        private void Finish()
        {
            if (_stateMachine.MovementDirection.sqrMagnitude > 0.1f)
            {
                _stateMachine.ChangeState(_stateMachine.WalkState);
            }
            else
            {
                _stateMachine.ChangeState(_stateMachine.IdleState);
            }
        }
    }
}

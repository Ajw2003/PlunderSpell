using Plunderspell.Status;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Levo's lift on the fresh guard. The guard has no physics body, so the spell's impulse
    /// (<see cref="StatusEffectReceiver.Levitate"/>) has nothing to push: while levitating the guard is raised
    /// by hand, turning slowly, and when the spell ends it falls back to where the lift began. Ported from the
    /// legacy <c>CastleGuard.UpdateLevitation</c>; its fall damage stays dropped (docs/plans/guard-core-inventory.md).
    /// The navigation service is paused meanwhile (Stunned state), so this is the only thing moving the guard.
    /// </summary>
    public sealed class GuardLift
    {
        private const float LiftHeight = 1.8f;
        private const float LiftSpeed = 3f;
        private const float TurnDegreesPerSecond = 45f;

        private readonly Transform _body;
        private readonly StatusEffectReceiver _status;
        private bool _floating;
        private bool _falling;
        private float _baseHeight;
        private float _fallSpeed;

        public GuardLift(Transform body, StatusEffectReceiver status)
        {
            _body = body;
            _status = status;
        }

        /// <summary>True while the guard is held up or still falling back down.</summary>
        public bool IsAirborne => _floating || _falling;

        public void Step(float deltaTime)
        {
            if (_status != null && _status.IsLevitating)
                Float(deltaTime);
            else if (_floating || _falling)
                Fall(deltaTime);
        }

        private void Float(float deltaTime)
        {
            if (!_floating)
            {
                _floating = true;
                _falling = false;
                _baseHeight = _body.position.y;
            }
            Vector3 position = _body.position;
            position.y = Mathf.MoveTowards(position.y, _baseHeight + LiftHeight, LiftSpeed * deltaTime);
            _body.position = position;
            _body.Rotate(Vector3.up, TurnDegreesPerSecond * deltaTime, Space.World);
        }

        // Straight back down to the lift's start: the guard rose vertically, so that is the floor it left.
        private void Fall(float deltaTime)
        {
            if (_floating)
            {
                _floating = false;
                _falling = true;
                _fallSpeed = 0f;
            }
            _fallSpeed -= Physics.gravity.y * deltaTime;
            Vector3 position = _body.position;
            position.y = Mathf.Max(_baseHeight, position.y - _fallSpeed * deltaTime);
            _body.position = position;
            if (position.y <= _baseHeight)
                _falling = false;
        }
    }
}

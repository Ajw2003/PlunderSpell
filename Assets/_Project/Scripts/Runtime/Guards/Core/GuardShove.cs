using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The Frango knock-back. A guard has no Rigidbody (the navigation service moves its transform), so
    /// an impulse does nothing: the guard is carried along the shove direction for a moment instead,
    /// and a capsule cast stops it at a wall or a player so a shove can never push anyone through one.
    /// </summary>
    public sealed class GuardShove
    {
        private const float Seconds = 0.25f;
        private const int HitBufferSize = 8;
        private const float SkinWidth = 0.03f;

        private readonly Transform _body;
        private readonly GuardTuning _tuning;
        private readonly RaycastHit[] _hits = new RaycastHit[HitBufferSize];
        private Vector3 _velocity;
        private float _timeLeft;

        public GuardShove(Transform body, GuardTuning tuning)
        {
            _body = body;
            _tuning = tuning;
        }

        public bool IsShoving => _timeLeft > 0f;

        /// <summary>Starts knocking the guard back by <paramref name="metres"/> over a moment.</summary>
        public void Start(Vector3 metres)
        {
            _velocity = new Vector3(metres.x, 0f, metres.z) / Seconds;
            _timeLeft = Seconds;
        }

        /// <summary>Moves the guard along the shove for this frame, cut short by whatever is in the way.</summary>
        public void Step(float deltaTime)
        {
            if (_timeLeft <= 0f)
                return;

            _timeLeft -= deltaTime;
            float distance = _velocity.magnitude * deltaTime;
            if (distance <= 0f)
                return;

            Vector3 direction = _velocity / _velocity.magnitude;
            _body.position += direction * AllowedDistance(direction, distance);
        }

        private float AllowedDistance(Vector3 direction, float distance)
        {
            Vector3 bottom = _body.position + Vector3.up * _tuning.BodyRadius;
            Vector3 top = _body.position + Vector3.up * (_tuning.BodyHeight - _tuning.BodyRadius);
            int count = Physics.CapsuleCastNonAlloc(bottom, top, _tuning.BodyRadius, direction, _hits,
                distance + SkinWidth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float nearest = distance + SkinWidth;
            for (int i = 0; i < count; i++)
            {
                bool own = _hits[i].collider.transform.IsChildOf(_body);
                if (!own && _hits[i].distance < nearest)
                    nearest = _hits[i].distance;
            }
            return Mathf.Clamp(nearest - SkinWidth, 0f, distance);
        }
    }
}

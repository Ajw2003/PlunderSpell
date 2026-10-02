using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The look of a death (#213): the body falls over, lies still, then shrinks away as dust puffs out.
    /// It is a scripted tween, not a ragdoll, because neither guard prefab has bones or joints (a real
    /// ragdoll waits on #141) and the fresh guard has no Rigidbody. Shrinking stands in for a dissolve:
    /// no dissolve shader exists in the project and a shared material cannot be faded per guard.
    /// Advance it with <see cref="Step"/>; it allocates nothing per frame.
    /// </summary>
    public sealed class GuardDeathVisual
    {
        private const float ToppleDegrees = 90f;

        private readonly Transform _body;
        private readonly GuardTuning _tuning;
        private readonly GuardDust _dust;
        private Quaternion _upright;
        private Vector3 _fullScale;
        private Vector3 _footPosition;
        private Vector3 _fallAxis;
        private float _elapsed;
        private bool _burst;

        public GuardDeathVisual(Transform body, GuardTuning tuning)
        {
            _body = body;
            _tuning = tuning;
            _dust = new GuardDust(body);
        }

        public bool HasStarted { get; private set; }

        /// <summary>True once the body has shrunk away and can be removed.</summary>
        public bool IsFinished => _elapsed >= _tuning.ToppleSeconds + _tuning.LingerSeconds + _tuning.FadeSeconds;

        /// <summary>Remembers the upright pose. The body falls to the side, so the fall axis is its own right.</summary>
        public void Begin()
        {
            HasStarted = true;
            _upright = _body.rotation;
            _fullScale = _body.localScale;
            _footPosition = _body.position;
            _fallAxis = _body.right;
            _elapsed = 0f;
        }

        public void Step(float deltaTime)
        {
            if (!HasStarted)
                return;

            _elapsed += deltaTime;
            float fallen = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, _tuning.ToppleSeconds));
            ApplyTopple(fallen * fallen);
            ApplyFade();
        }

        // Squared so it starts slow and ends fast, like a body falling. Lifted by the body radius so the
        // lying body rests on the floor instead of sinking half into it, as the pivot is at the feet.
        private void ApplyTopple(float easedProgress)
        {
            _body.rotation = Quaternion.AngleAxis(ToppleDegrees * easedProgress, _fallAxis) * _upright;
            _body.position = _footPosition + Vector3.up * (_tuning.BodyRadius * easedProgress);
        }

        private void ApplyFade()
        {
            float fadeStart = _tuning.ToppleSeconds + _tuning.LingerSeconds;
            if (_elapsed < fadeStart)
                return;

            if (!_burst)
            {
                _burst = true;
                _dust.Burst(_tuning.DustParticleCount, _tuning.BodyHeight * 0.5f);
            }
            float faded = Mathf.Clamp01((_elapsed - fadeStart) / Mathf.Max(0.0001f, _tuning.FadeSeconds));
            _body.localScale = _fullScale * (1f - faded);
        }
    }
}

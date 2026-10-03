using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Plays a guard's death on every peer (#213). A client never runs the guard's states, but it does see
    /// the replicated state, so this watches that state: the moment it reads Dead, each peer runs the same
    /// topple and fade locally (<see cref="GuardDeathVisual"/>). Only the server then despawns the guard,
    /// by destroying it, which is the path the legacy guard used and PurrNet turns into a network despawn.
    /// Idle for a living guard.
    /// </summary>
    [RequireComponent(typeof(Guard))]
    public sealed class GuardDeathPlayback : MonoBehaviour
    {
        private Guard _guard;
        private GuardDeathVisual _visual;
        private bool _removalRequested;

        public bool HasStarted => _visual != null && _visual.HasStarted;

        private void Update() => Step(Time.deltaTime);

        /// <summary>Advances the death by one step. Public so a test can run it without frames.</summary>
        public void Step(float deltaTime)
        {
            if (_guard == null)
                _guard = GetComponent<Guard>();
            if (_guard.State != GuardAlertState.Dead)
                return;

            if (_visual == null)
                _visual = new GuardDeathVisual(transform, _guard.Tuning);
            if (!_visual.HasStarted)
                _visual.Begin();

            _visual.Step(deltaTime);
            if (_visual.IsFinished && _guard.IsAuthority && !_removalRequested)
            {
                _removalRequested = true;
                Destroy(gameObject);
            }
        }
    }
}

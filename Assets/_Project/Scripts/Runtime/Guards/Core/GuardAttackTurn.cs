using Code.Scripts.EventSystems;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A guard's side of the attack turns (#210): it asks the director for a turn by event and remembers
    /// the answer. It listens only while the guard is in Combat (<see cref="Begin"/> to <see cref="End"/>),
    /// so a patrolling guard costs the director nothing. Without a director there is nobody to share with,
    /// so the guard simply has its turn.
    /// </summary>
    public sealed class GuardAttackTurn
    {
        private readonly Guard _guard;
        private EnemyDirector _director;
        private bool _granted;

        public GuardAttackTurn(Guard guard)
        {
            _guard = guard;
        }

        /// <summary>True while the guard may attack. The director takes a turn back on a timeout, so this
        /// asks it rather than trusting the last answer.</summary>
        public bool HasTurn => _director == null || (_granted && _director.AttackTurns.Holds(_guard));

        /// <summary>Starts listening for answers to this guard's requests.</summary>
        public void Begin()
        {
            _director = _guard.Link.Director;
            _granted = false;
            if (_director == null)
                return;

            EventManager.Instance?.Subscribe(this, (AttackTurnGranted answer) => OnGranted(answer));
            EventManager.Instance?.Subscribe(this, (AttackTurnDenied answer) => OnDenied(answer));
        }

        /// <summary>Gives any turn back and stops listening.</summary>
        public void End()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            if (_director == null)
                return;

            Release();
            _director = null;
        }

        /// <summary>Asks for a turn on <paramref name="target"/>. The answer arrives before this returns.</summary>
        public void Request(Transform target, bool ranged)
        {
            EventManager.Instance?.Publish(new AttackTurnRequested(_guard, target, ranged));
        }

        /// <summary>Gives the turn back so another guard can attack.</summary>
        public void Release()
        {
            if (_director != null && _granted)
                EventManager.Instance?.Publish(new AttackTurnReleased(_guard));
            _granted = false;
        }

        private void OnGranted(AttackTurnGranted answer)
        {
            if (answer.Guard == _guard)
                _granted = true;
        }

        private void OnDenied(AttackTurnDenied answer)
        {
            if (answer.Guard == _guard)
                _granted = false;
        }
    }
}

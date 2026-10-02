using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// Hands out attack turns per target (#210), so three guards in reach of one player take it in turns
    /// instead of all striking at once. Guards never ask each other: a guard publishes
    /// <see cref="AttackTurnRequested"/> on the director and hears <see cref="AttackTurnGranted"/> or
    /// <see cref="AttackTurnDenied"/> back. A turn ends when the guard releases it, dies, is removed from the
    /// director, or the timeout runs out. Server-side only; the list is preallocated, so no frame allocates.
    /// </summary>
    public sealed class AttackTurnMediator
    {
        private struct Turn
        {
            public Component Guard;
            public Transform Target;
            public bool Ranged;
            public float SecondsLeft;
        }

        private readonly EnemyDirector _director;
        private readonly AttackTurnTuning _tuning;
        private readonly List<Turn> _turns = new List<Turn>(16);

        public AttackTurnMediator(EnemyDirector director, AttackTurnTuning tuning)
        {
            _director = director;
            _tuning = tuning;
        }

        /// <summary>Turns held right now, for tests and the debug overlay.</summary>
        public int ActiveTurns => _turns.Count;

        /// <summary>True while <paramref name="guard"/> holds a turn.</summary>
        public bool Holds(Component guard) => IndexOf(guard) >= 0;

        /// <summary>How many guards of the given kind hold a turn on <paramref name="target"/>.</summary>
        public int TurnsOn(Transform target, bool ranged)
        {
            int count = 0;
            for (int i = 0; i < _turns.Count; i++)
            {
                if (_turns[i].Target == target && _turns[i].Ranged == ranged)
                    count++;
            }
            return count;
        }

        /// <summary>Answers a request: a guard that already holds a turn on the target keeps it, otherwise it
        /// gets one when the limit for its kind is not reached.</summary>
        public void Handle(AttackTurnRequested request)
        {
            int held = IndexOf(request.Guard);
            if (held >= 0 && _turns[held].Target == request.Target)
            {
                _director.Publish(new AttackTurnGranted(request.Guard, request.Target));
                return;
            }

            Release(request.Guard);
            int limit = request.Ranged ? _tuning.RangedTurnsPerTarget : _tuning.MeleeTurnsPerTarget;
            if (TurnsOn(request.Target, request.Ranged) >= limit)
            {
                _director.Publish(new AttackTurnDenied(request.Guard, request.Target));
                return;
            }

            _turns.Add(new Turn
            {
                Guard = request.Guard,
                Target = request.Target,
                Ranged = request.Ranged,
                SecondsLeft = _tuning.TurnTimeoutSeconds
            });
            _director.Publish(new AttackTurnGranted(request.Guard, request.Target));
        }

        /// <summary>Takes back whatever turn <paramref name="guard"/> holds. Silent: the caller knows.</summary>
        public void Release(Component guard)
        {
            int index = IndexOf(guard);
            if (index >= 0)
                _turns.RemoveAt(index);
        }

        /// <summary>Counts the timeouts down. A turn whose holder or target is gone is dropped too.</summary>
        public void Tick(float deltaTime)
        {
            for (int i = _turns.Count - 1; i >= 0; i--)
            {
                Turn turn = _turns[i];
                turn.SecondsLeft -= deltaTime;
                bool gone = turn.Guard == null || turn.Target == null;
                if (gone || turn.SecondsLeft <= 0f)
                    _turns.RemoveAt(i);
                else
                    _turns[i] = turn;
            }
        }

        private int IndexOf(Component guard)
        {
            for (int i = 0; i < _turns.Count; i++)
            {
                if (_turns[i].Guard == guard)
                    return i;
            }
            return -1;
        }
    }
}

using System;
using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Investigate (#208): walk to where a noise, a sighting or the hue and cry pointed, stand and look
    /// around for a moment, then go back to Patrol if nothing turned up. This replaces the old Search
    /// sweep. A newer lead at least as strong re-targets the walk; a weaker one is dropped. A blocked
    /// walk (a closed door, an unreachable spot) is not worth retrying, so the guard gives up.
    ///
    /// The guard only stands while it looks: turning in place would mean moving the transform, and
    /// movement belongs to the navigation service alone.
    /// </summary>
    public sealed class InvestigateState : GuardState
    {
        private Vector3 _spot;
        private float _leadStrength;
        private float _lookTimeLeft;
        private bool _arrived;
        private bool _gaveUp;
        private Transform _lastSighting;

        public InvestigateState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Investigating;

        /// <summary>
        /// HAND-OFF TO CHASE (#209). Raised with the player's transform once per sighting while the guard
        /// investigates. #209 replaces this with a return of its Chase state from
        /// <see cref="ReactToPlayerSeen"/>; until then the guard keeps investigating.
        /// </summary>
        public event Action<Transform> PlayerSeen;

        /// <summary>Where the guard is heading or looking, for tests and the debug overlay.</summary>
        public Vector3 Spot => _spot;

        public override void Enter()
        {
            _gaveUp = false;
            _lastSighting = null;
            Context.Navigator.Reached += OnReached;
            Context.Navigator.RouteBlocked += OnBlocked;
            GoToWaitingLead();
        }

        public override void Exit()
        {
            Context.Navigator.Stop();
            Context.Navigator.Reached -= OnReached;
            Context.Navigator.RouteBlocked -= OnBlocked;
        }

        public override State<Guard> Tick(float deltaTime)
        {
            State<Guard> chase = NoticePlayer();
            if (chase != null)
                return chase;

            RetargetOnNewLead();
            if (_gaveUp)
                return Context.States.Patrol;

            if (!_arrived)
                return this;

            _lookTimeLeft -= deltaTime;
            return _lookTimeLeft <= 0f ? Context.States.Patrol : this;
        }

        private State<Guard> NoticePlayer()
        {
            Transform seen = Context.Sight.Visible;
            State<Guard> next = null;
            if (seen != null && seen != _lastSighting)
                next = ReactToPlayerSeen(seen);
            _lastSighting = seen;
            return next;
        }

        // The one place #209 changes: return the Chase state here once it exists.
        private State<Guard> ReactToPlayerSeen(Transform player)
        {
            PlayerSeen?.Invoke(player);
            return null;
        }

        // A weaker lead than the one being followed is thrown away so it does not send the guard
        // out again the moment this investigation ends.
        private void RetargetOnNewLead()
        {
            if (!Context.Leads.HasLead)
                return;

            if (Context.Leads.Strength >= _leadStrength)
                GoToWaitingLead();
            else
                Context.Leads.Clear();
        }

        private void GoToWaitingLead()
        {
            _leadStrength = Context.Leads.Strength;
            _spot = Context.Leads.Take();
            _arrived = false;
            _lookTimeLeft = Context.Tuning.InvestigateLookSeconds;
            Context.Navigator.MoveTo(_spot, Context.Tuning.InvestigateSpeed, MoveReason.Investigate);
        }

        private void OnReached(Arrived arrived) => _arrived = true;

        // Any block means the spot cannot be got to now: give up rather than retry.
        private void OnBlocked(Blocked blocked) => _gaveUp = true;
    }
}

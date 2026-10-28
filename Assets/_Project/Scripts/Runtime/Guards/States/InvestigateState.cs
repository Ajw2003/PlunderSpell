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
    /// The guard faces the spot before it walks, and at the spot it sweeps 60 degrees left then right
    /// (<see cref="GuardLookAround"/>, #483). Only yaw changes; position belongs to the navigation service.
    /// </summary>
    public sealed class InvestigateState : GuardState
    {
        private Vector3 _spot;
        private Vector3 _heading;
        private bool _hasHeading;
        private float _leadStrength;
        private float _lookTimeLeft;
        private bool _arrived;
        private bool _gaveUp;
        private Transform _lastSighting;
        private readonly GuardNearestReachableWalk _detour;
        private bool _onDetour;
        private bool _walkPending;
        private readonly GuardLookAround _look;

        public InvestigateState(Guard guard) : base(guard)
        {
            _look = new GuardLookAround(guard);
            _detour = new GuardNearestReachableWalk(guard);
        }

        public override GuardAlertState AlertState => GuardAlertState.Investigating;

        /// <summary>
        /// Raised with the player's transform once per sighting while the guard investigates, just before
        /// it hands over to <see cref="ChaseState"/> (#209).
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
            _look.Cancel();
            _walkPending = false;
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

            _look.Tick(deltaTime);
            if (_walkPending && !_look.IsTurning)
                StartWalk();

            if (!_arrived)
                return this;

            _lookTimeLeft -= deltaTime;
            return _lookTimeLeft <= 0f && !_look.IsTurning ? Context.States.Patrol : this;
        }

        private void StartWalk()
        {
            _walkPending = false;
            Context.Navigator.MoveTo(_spot, LeadSpeed(), MoveReason.Investigate);
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

        private State<Guard> ReactToPlayerSeen(Transform player)
        {
            PlayerSeen?.Invoke(player);
            Context.States.Chase.Follow(player);
            return Context.States.Chase;
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
            _hasHeading = Context.Leads.HasHeading;
            _heading = Context.Leads.Heading;
            _spot = Context.Leads.Take();
            _arrived = false;
            _lookTimeLeft = Context.Tuning.InvestigateLookSeconds;
            _onDetour = false;
            _walkPending = true;
            _look.BeginTurnToward(_spot);
        }

        private void OnReached(Arrived arrived)
        {
            _arrived = true;
            // At a doorway the guard faces back toward the sound, then looks about from there (#485).
            if (_hasHeading)
                _look.BeginSweepFacing(_heading);
            else
                _look.BeginSweep();
        }

        // A cry or the hue and cry is followed at chase speed; a mere noise at investigate speed.
        private float LeadSpeed() =>
            _leadStrength >= GuardLeads.CryStrength ? Context.Tuning.ChaseSpeed : Context.Tuning.InvestigateSpeed;

        // No route to the spot: look from the nearest floor the guard can reach (#481). Any other block
        // means the spot cannot be got to now, so give up rather than retry.
        private void OnBlocked(Blocked blocked)
        {
            _onDetour = !_onDetour && GuardNearestReachableWalk.IsMissingRoute(blocked)
                && _detour.TryStart(_spot, LeadSpeed(), MoveReason.Investigate);
            _gaveUp = !_onDetour;
        }
    }
}

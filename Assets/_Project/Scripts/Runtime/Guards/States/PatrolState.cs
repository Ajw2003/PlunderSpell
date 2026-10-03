using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Patrol (#207): walk a round of three or more random points reachable from the post, pausing a
    /// moment at each, then plan a fresh round. The guard is never idle for longer than a pause.
    ///
    /// A blocked walk is handled by cause: a closed door means the point is not worth trying (a barred
    /// door will not open mid-raid), anything else gets the point swapped for another. Seeing or hearing
    /// something becomes a lead in <see cref="GuardLeads"/>; a waiting lead (a noise, the hue and cry, or a
    /// new sighting) sends the guard to <see cref="InvestigateState"/> (#208).
    /// </summary>
    public sealed class PatrolState : GuardState
    {
        private readonly GuardPatrolRoute _route = new GuardPatrolRoute();
        private readonly GuardPatrolPlanner _planner;
        private float _pauseLeft;
        private Transform _lastSighting;

        public PatrolState(Guard guard) : base(guard)
        {
            _planner = new GuardPatrolPlanner(guard.Tuning, () => guard.Random);
        }

        public override GuardAlertState AlertState => GuardAlertState.Patrolling;

        /// <summary>The points of the current round, for tests and the debug overlay.</summary>
        public GuardPatrolRoute Route => _route;

        public override void Enter()
        {
            _route.Clear();
            _pauseLeft = 0f;
            _lastSighting = null;
            Context.Navigator.Reached += OnReached;
            Context.Navigator.RouteBlocked += OnBlocked;
        }

        public override void Exit()
        {
            Context.Navigator.Stop();
            Context.Navigator.Reached -= OnReached;
            Context.Navigator.RouteBlocked -= OnBlocked;
        }

        public override State<Guard> Tick(float deltaTime)
        {
            _pauseLeft -= deltaTime;
            NoticeSighting();
            if (Context.Leads.HasLead)
                return Context.States.Investigate;
            if (_pauseLeft <= 0f && !Context.Navigator.IsMoving)
                WalkToNextPoint();
            return this;
        }

        // Raised once per sighting, not every look, so a guard that keeps seeing the same player does not
        // flood whoever handles it. Losing sight forgets the sighting, so the next one is news again.
        private void NoticeSighting()
        {
            Transform seen = Context.Sight.Visible;
            if (seen != null && seen != _lastSighting)
                Context.Leads.Offer(seen.position, GuardLeads.SightingStrength);
            _lastSighting = seen;
        }

        private void WalkToNextPoint()
        {
            IGuardNavigationMap map = Context.Link.Director != null ? Context.Link.Director.Navigation.Map : null;
            if (_route.IsFinished)
                _planner.PlanRound(map, Context.Home, _route);

            // No map, or a boxed-in post: wait a pause and try again rather than spin every frame.
            if (_route.IsFinished)
            {
                PauseBeforeNextPoint();
                return;
            }
            Context.Navigator.MoveTo(_route.Current, Context.Tuning.PatrolSpeed, MoveReason.Patrol);
        }

        private void OnReached(Arrived arrived)
        {
            _route.Advance();
            PauseBeforeNextPoint();
        }

        private void OnBlocked(Blocked blocked)
        {
            if (_route.IsFinished)
                return;

            if (blocked.Reason == BlockedReason.DoorClosed)
                _route.DropCurrent();
            else
                SwapCurrentPoint();
            PauseBeforeNextPoint();
        }

        private void SwapCurrentPoint()
        {
            IGuardNavigationMap map = Context.Link.Director.Navigation.Map;
            if (_planner.TryPickPoint(map, Context.Home, _route, out Vector3 other))
                _route.ReplaceCurrent(other);
            else
                _route.DropCurrent();
        }

        private void PauseBeforeNextPoint() => _pauseLeft = Context.Tuning.PatrolPauseSeconds;
    }
}

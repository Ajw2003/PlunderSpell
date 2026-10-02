using StateMachine;
using UnityEngine;
using Plunderspell.Alarm;

namespace Plunderspell.Guards
{
    /// <summary>
    /// PLACEHOLDER for the Patrol state (#207). It exists so the core can stand up and move through the
    /// navigation service: it picks a point near where the guard was posted, asks to walk there, waits a
    /// moment on arrival (or when blocked), and picks another. It does not yet choose 3+ reachable
    /// points, and it ignores noises, sightings and the hue and cry; Investigate (#208) and Chase (#209)
    /// are what react to those. Replace this class, do not extend it.
    /// </summary>
    public sealed class PlaceholderPatrolState : GuardState
    {
        private const float MinimumRadius = 3f;
        private const float MaximumRadius = 8f;
        private const float PauseSeconds = 1f;

        private float _pauseLeft;
        private bool _needsPoint;

        public PlaceholderPatrolState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Patrolling;

        public override void Enter()
        {
            _needsPoint = true;
            _pauseLeft = 0f;
            Context.Navigator.Reached += OnMoveFinished;
            Context.Navigator.RouteBlocked += OnMoveBlocked;
        }

        public override void Exit()
        {
            Context.Navigator.Reached -= OnMoveFinished;
            Context.Navigator.RouteBlocked -= OnMoveBlocked;
        }

        public override State<Guard> Tick(float deltaTime)
        {
            _pauseLeft -= deltaTime;
            if (_needsPoint && _pauseLeft <= 0f)
                RequestNextPoint();
            return this;
        }

        private void RequestNextPoint()
        {
            _needsPoint = false;
            Vector2 around = Random.insideUnitCircle.normalized * Random.Range(MinimumRadius, MaximumRadius);
            Vector3 point = Context.Home + new Vector3(around.x, 0f, around.y);
            Context.Navigator.MoveTo(point, Context.Tuning.PatrolSpeed, MoveReason.Patrol);
        }

        private void OnMoveFinished(Arrived arrived) => WaitThenPickAgain();

        private void OnMoveBlocked(Blocked blocked) => WaitThenPickAgain();

        private void WaitThenPickAgain()
        {
            _needsPoint = true;
            _pauseLeft = PauseSeconds;
        }
    }
}

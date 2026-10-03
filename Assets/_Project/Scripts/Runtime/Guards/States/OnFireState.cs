using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// OnFire (#212): Ignis has set the guard alight, so it panics. It runs to random reachable points
    /// around where it stands at <see cref="GuardTuning.PanicSpeed"/>, picking a new one each time it
    /// arrives or is blocked. It does not attack, ignores every lead, and gives back any attack turn.
    ///
    /// The burn damage is not done here. <c>StatusEffectReceiver.Tick</c> applies it to the guard's health
    /// in whole points, and a lethal tick makes <see cref="Guard"/> enter Dead on its own; this state only
    /// watches the status and decides where to go when the fire ends:
    /// a player in sight and in reach goes to Combat, a player in sight only goes to Chase, and otherwise
    /// the guard recovers as after a stun (<see cref="GuardRecovery"/>).
    /// </summary>
    public sealed class OnFireState : GuardState
    {
        private readonly GuardPatrolPlanner _planner;
        private readonly GuardPatrolRoute _noPointsYet = new GuardPatrolRoute();
        private float _retrySecondsLeft;

        public OnFireState(Guard guard) : base(guard)
        {
            _planner = new GuardPatrolPlanner(guard.Tuning, () => guard.Random);
        }

        public override GuardAlertState AlertState => GuardAlertState.OnFire;

        public override void Enter()
        {
            Context.Navigator.Stop();
            // A guard that caught fire mid-fight must not keep blocking the next guard's strike.
            Context.Link.Director?.AttackTurns.Release(Context);
            _retrySecondsLeft = 0f;
            Context.Navigator.RouteBlocked += OnBlocked;
        }

        public override void Exit()
        {
            Context.Navigator.Stop();
            Context.Navigator.RouteBlocked -= OnBlocked;
        }

        public override State<Guard> Tick(float deltaTime)
        {
            if (Context.IsDead)
                return Context.States.Dead;
            if (!Context.Status.IsBurning)
                return AfterTheFire();

            _retrySecondsLeft -= deltaTime;
            // Arrived and Blocked both clear the destination, so "not moving" is the cue for a new point.
            if (_retrySecondsLeft <= 0f && !Context.Navigator.IsMoving)
                RunToRandomPoint();
            return this;
        }

        private void RunToRandomPoint()
        {
            IGuardNavigationMap map = Context.Link.Director != null ? Context.Link.Director.Navigation.Map : null;
            // The panic is around where the guard is now, not its post, so it runs about wherever the fire caught it.
            if (_planner.TryPickPoint(map, Context.transform.position, _noPointsYet, out Vector3 point))
                Context.Navigator.MoveTo(point, Context.Tuning.PanicSpeed, MoveReason.Patrol);
            else
                _retrySecondsLeft = Context.Tuning.PanicRetrySeconds;
        }

        private void OnBlocked(Blocked blocked) => _retrySecondsLeft = Context.Tuning.PanicRetrySeconds;

        private State<Guard> AfterTheFire()
        {
            Transform seen = Context.Sight.Visible;
            if (seen == null)
                return GuardRecovery.PatrolOrInvestigate(Context);

            float reach = Context.RangedAttack.IsRanged ? Context.Tuning.RangedEngageRange : Context.Tuning.MeleeReach;
            if (Vector3.Distance(Context.transform.position, seen.position) <= reach)
            {
                Context.States.Combat.Engage(seen);
                return Context.States.Combat;
            }

            Context.States.Chase.Follow(seen);
            return Context.States.Chase;
        }
    }
}

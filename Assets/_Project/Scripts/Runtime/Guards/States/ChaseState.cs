using System;
using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Chase (#209): follow the player we saw at chase speed. The move is re-planned as the player moves,
    /// but only on a timer and only when the player has gone somewhere new, so the route service is not
    /// asked every frame. Ranged guards shoot while they run. Losing sight sends the guard to
    /// Investigate at the last place it saw the player. Coming within reach is the Combat hand-off (#210).
    ///
    /// Movement stays with the navigation service: its capsule sweep stops the guard short of a player,
    /// so a player pinned against a wall is never pushed (#200). A Blocked answer is not an exit here:
    /// the next re-plan simply tries again.
    /// </summary>
    public sealed class ChaseState : GuardState
    {
        private Transform _target;
        private Vector3 _lastSeenSpot;
        private float _secondsSinceSeen;
        private float _secondsToRetarget;
        private bool _inReach;

        public ChaseState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Chasing;

        /// <summary>
        /// HAND-OFF TO COMBAT (#210). Raised with the player's transform once each time the guard comes
        /// within reach (melee reach, or the ranged engage range for a ranged guard). #210 replaces this
        /// with a return of its Combat state from <see cref="ReactToInReach"/>; until then the guard
        /// keeps chasing, standing at the player.
        /// </summary>
        public event Action<Transform> InReach;

        /// <summary>The player being chased, for tests and the debug overlay.</summary>
        public Transform Target => _target;

        /// <summary>Names the player to chase. Call before the guard enters this state.</summary>
        public void Follow(Transform player) => _target = player;

        public override void Enter()
        {
            _lastSeenSpot = _target.position;
            _secondsSinceSeen = 0f;
            _inReach = false;
            MoveToward(_lastSeenSpot);
            Context.Link.Director?.Publish(new IntruderSpotted(Context, _target, _lastSeenSpot));
        }

        public override void Exit()
        {
            Context.Navigator.Stop();
            Context.Link.Director?.Publish(new IntruderLost(Context, _lastSeenSpot));
        }

        public override State<Guard> Tick(float deltaTime)
        {
            if (_target == null)
                return GiveUp();

            Transform seen = Context.Sight.Visible;
            if (seen == null)
                return KeepRunningToLastSeenSpot(deltaTime);

            _target = seen;
            _lastSeenSpot = seen.position;
            _secondsSinceSeen = 0f;
            RetargetWhenDue(deltaTime);
            Context.RangedAttack.TryFire(seen, deltaTime);
            return NoticeReach(seen);
        }

        // Sight is a look every 80 ms or so and can miss for one look, so a short grace keeps a flicker
        // at the edge of view from ending the chase. The guard heads for where it last saw the player,
        // never to the player's real position, which it cannot know.
        private State<Guard> KeepRunningToLastSeenSpot(float deltaTime)
        {
            _secondsSinceSeen += deltaTime;
            _inReach = false;
            return _secondsSinceSeen >= Context.Tuning.ChaseLoseSightSeconds ? GiveUp() : this;
        }

        private State<Guard> GiveUp()
        {
            // The strongest lead, so Investigate goes straight to the last seen spot.
            Context.Leads.Offer(_lastSeenSpot, GuardLeads.SightingStrength);
            return Context.States.Investigate;
        }

        private void RetargetWhenDue(float deltaTime)
        {
            _secondsToRetarget -= deltaTime;
            if (_secondsToRetarget > 0f)
                return;

            _secondsToRetarget = Context.Tuning.ChaseRetargetSeconds;
            Vector3? planned = Context.Navigator.Destination;
            float moved = planned.HasValue ? Vector3.Distance(planned.Value, _lastSeenSpot) : float.MaxValue;
            if (moved > Context.Tuning.ChaseRetargetDistance)
                MoveToward(_lastSeenSpot);
        }

        private void MoveToward(Vector3 spot)
        {
            _secondsToRetarget = Context.Tuning.ChaseRetargetSeconds;
            Context.Navigator.MoveTo(spot, Context.Tuning.ChaseSpeed, MoveReason.Chase);
        }

        private State<Guard> NoticeReach(Transform seen)
        {
            float reach = Context.RangedAttack.IsRanged ? Context.Tuning.RangedEngageRange : Context.Tuning.MeleeReach;
            bool inReach = Vector3.Distance(Context.transform.position, seen.position) <= reach;
            State<Guard> next = null;
            if (inReach && !_inReach)
                next = ReactToInReach(seen);
            _inReach = inReach;
            return next;
        }

        // The one place #210 changes: return the Combat state here once it exists.
        private State<Guard> ReactToInReach(Transform player)
        {
            InReach?.Invoke(player);
            return null;
        }
    }
}

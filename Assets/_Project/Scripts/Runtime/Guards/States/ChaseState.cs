using Code.Scripts.EventSystems;
using System;
using Plunderspell.Alarm;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Chase (#209): follow the player we saw at chase speed. The move is re-planned as the player moves,
    /// but only on a timer and only when the player has gone somewhere new, so the route service is not
    /// asked every frame. Ranged guards shoot while they run, never through a teammate (the check is in
    /// <see cref="GuardRangedAttack"/>, #210). Losing sight sends the guard to Investigate at the last place
    /// it saw the player. Coming within reach hands over to <see cref="CombatState"/> (#210).
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
        private bool _alreadySpotted;
        private readonly GuardNearestReachableWalk _detour;
        private bool _onDetour;
        private bool _holdBelowDue;

        public ChaseState(Guard guard) : base(guard)
        {
            _detour = new GuardNearestReachableWalk(guard);
        }

        public override GuardAlertState AlertState => GuardAlertState.Chasing;

        /// <summary>
        /// Raised with the player's transform once each time the guard comes within reach (melee reach,
        /// or the ranged engage range for a ranged guard), just before the hand-off to Combat (#210).
        /// </summary>
        public event Action<Transform> InReach;

        /// <summary>The player being chased, for tests and the debug overlay.</summary>
        public Transform Target => _target;

        /// <summary>Names the player to chase. Call before the guard enters this state. Pass
        /// <paramref name="alreadySpotted"/> when coming back from Combat, so the sighting is not scored twice.</summary>
        public void Follow(Transform player, bool alreadySpotted = false)
        {
            _target = player;
            _alreadySpotted = alreadySpotted;
        }

        public override void Enter()
        {
            _lastSeenSpot = _target.position;
            Context.LastKnown.Sighted(_target, _lastSeenSpot);
            _secondsSinceSeen = 0f;
            _inReach = false;
            _holdBelowDue = false;
            Context.Navigator.RouteBlocked += OnBlocked;
            Context.Navigator.Reached += OnReached;
            MoveToward(_lastSeenSpot);
            if (!_alreadySpotted)
                Context.Cry.Raise();
            EventManager.Instance?.Publish(new IntruderSpotted(Context, _target, _lastSeenSpot, !_alreadySpotted));
        }

        public override void Exit()
        {
            Context.Navigator.RouteBlocked -= OnBlocked;
            Context.Navigator.Reached -= OnReached;
            Context.Navigator.Stop();
            EventManager.Instance?.Publish(new IntruderLost(Context, _lastSeenSpot));
        }

        public override State<Guard> Tick(float deltaTime)
        {
            if (_target == null)
                return GiveUp();

            Transform seen = Context.Sight.Visible;
            if (seen == null)
                return KeepRunningToLastSeenSpot(deltaTime);

            // A Lantern Warden that confirms a sight turns into the Wick (#417).
            if (Context.TrueForm.TurnOnSight(seen))
                return null;

            _target = seen;
            _lastSeenSpot = seen.position;
            Context.LastKnown.Sighted(seen, _lastSeenSpot);
            _secondsSinceSeen = 0f;
            // A melee guard that cannot get at the player stops chasing and throws instead (#237).
            // Also when the route failed and the nearest reachable floor did not get it any closer (#481).
            if (!Context.RangedAttack.IsRanged && (Context.Reach.IsUnreachable(seen, deltaTime) || _holdBelowDue))
            {
                Context.States.HoldBelow.Hold(seen);
                return Context.States.HoldBelow;
            }

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
            // At chase speed (#488) a melee guard reaches the foot of a ledge before the reach check confirms,
            // then loses the player overhead; it still knows they were up there, so it holds below.
            if (!Context.RangedAttack.IsRanged && Context.Reach.IsUnreachable(_target, deltaTime))
            {
                Context.States.HoldBelow.Hold(_target);
                return Context.States.HoldBelow;
            }
            return _secondsSinceSeen >= Context.Tuning.ChaseLoseSightSeconds ? GiveUp() : this;
        }

        private State<Guard> GiveUp()
        {
            // The strongest lead, so Investigate goes straight to the last seen spot.
            Context.Leads.Offer(_lastSeenSpot, GuardLeads.SightingStrength);
            Context.LastKnown.Hunt(_target);
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

        // No route to the player: walk to the nearest floor the guard can reach. If it is already there
        // (or that walk fails too) a melee guard holds below; a ranged one keeps shooting from where it is.
        private void OnBlocked(Blocked blocked)
        {
            if (!GuardNearestReachableWalk.IsMissingRoute(blocked))
                return;

            _onDetour = !_onDetour && _detour.TryStart(_lastSeenSpot, Context.Tuning.ChaseSpeed, MoveReason.Chase);
            _holdBelowDue = !_onDetour;
        }

        // Having walked the long way round, try the player again from here.
        private void OnReached(Arrived arrived)
        {
            if (_onDetour)
                MoveToward(_lastSeenSpot);
        }

        private void MoveToward(Vector3 spot)
        {
            _onDetour = false;
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

        private State<Guard> ReactToInReach(Transform player)
        {
            InReach?.Invoke(player);
            Context.States.Combat.Engage(player);
            return Context.States.Combat;
        }
    }
}

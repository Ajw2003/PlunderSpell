using System.Collections.Generic;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// The director's navigation service (#222, docs/plans/bespoke-navigation.md Design 3). Server side
    /// only. It listens for <see cref="MoveRequest"/>, plans a smoothed route, and every
    /// <see cref="Tick"/> walks each registered guard along its route by setting its transform. It
    /// answers on the director's bus with <see cref="PathReady"/>, <see cref="Arrived"/> and
    /// <see cref="Blocked"/>. Each small job (planning, following, separating, sweeping, stepping) is a
    /// class of its own; this one only sequences them.
    ///
    /// Nothing here allocates per tick: movers sit in a list walked by index, events are readonly
    /// structs, and the sweep reuses one hit buffer. Allocation happens only when a request is planned.
    /// </summary>
    public sealed class GuardNavigationService
    {
        public const float DefaultRadius = 0.4f;
        public const float DefaultHeight = 1.8f;

        private readonly EnemyDirector _director;
        private readonly GuardNavigationTuning _tuning;
        private readonly List<GuardMover> _movers = new List<GuardMover>();
        private readonly GuardPathPlanner _planner = new GuardPathPlanner();
        private readonly GuardPathFollower _follower;
        private readonly GuardSeparation _separation;
        private readonly GuardMoverStepper _stepper;
        private IGuardNavigationMap _map;
        private int _planVersion;

        public GuardNavigationService(EnemyDirector director, GuardNavigationTuning tuning)
        {
            _director = director;
            _tuning = tuning;
            _follower = new GuardPathFollower(tuning);
            _separation = new GuardSeparation(tuning);
            _stepper = new GuardMoverStepper(tuning, new GuardSweep(tuning));
            director.OnMoveRequest += HandleMoveRequest;
        }

        public GuardPathPlanner Planner => _planner;

        public int MoverCount => _movers.Count;

        public GuardNavigationTuning Tuning => _tuning;

        /// <summary>The castle the service plans on, or null before <see cref="SetMap"/>. States read it to
        /// ask "can I get there" without sending a move request.</summary>
        public IGuardNavigationMap Map => _map;

        /// <summary>Gives the service the castle to plan on. Without one every request is answered Blocked.</summary>
        public void SetMap(IGuardNavigationMap map)
        {
            _map = map;
            _planVersion = map != null ? map.Version : 0;
            _planner.Clear();
        }

        /// <summary>Starts moving a guard's transform. A guard must be registered before it can be asked to move.</summary>
        public void Register(Component guard, float radius = DefaultRadius, float height = DefaultHeight)
        {
            if (guard != null && FindMover(guard) == null)
                _movers.Add(new GuardMover(guard, radius, height));
        }

        public void Unregister(Component guard)
        {
            GuardMover mover = FindMover(guard);
            if (mover != null)
                _movers.Remove(mover);
        }

        /// <summary>Stops a guard where it stands. No event is raised: the caller asked for it.</summary>
        public void Cancel(Component guard) => FindMover(guard)?.Stop();

        public bool IsMoving(Component guard) => FindMover(guard)?.IsMoving ?? false;

        private GuardMover FindMover(Component guard)
        {
            for (int i = 0; i < _movers.Count; i++)
            {
                if (_movers[i].Guard == guard)
                    return _movers[i];
            }
            return null;
        }

        private void HandleMoveRequest(MoveRequest request)
        {
            GuardMover mover = FindMover(request.Guard);
            if (mover == null)
            {
                Debug.LogWarning("[GuardNavigation] Move request from a guard that is not registered; ignored.");
                return;
            }
            mover.Stop(); // the old route is being replaced, so the door check below must not replan it first
            ReplanIfDoorsChanged(); // a door may have changed since the last tick, which would leave the cache stale
            mover.Destination = request.Destination;
            mover.Speed = request.Speed;
            mover.Reason = request.Reason;
            Plan(mover);
        }

        private void Plan(GuardMover mover)
        {
            mover.Stop();
            if (_map == null)
            {
                RaiseBlocked(mover, BlockedReason.NoMap);
                return;
            }
            if (!_planner.TryPlan(_map, mover.Position, mover.Destination, mover.Radius, out Vector3[] route, out BlockedReason failure))
            {
                RaiseBlocked(mover, failure);
                return;
            }
            mover.Path = route;
            _director.Publish(new PathReady(mover.Guard, mover.Destination, route.Length, GuardPathPlanner.LengthOf(route)));
        }

        /// <summary>Moves every registered guard one step. Called each frame by the director on the server.</summary>
        public void Tick(float deltaTime)
        {
            ReplanIfDoorsChanged();
            for (int i = 0; i < _movers.Count; i++)
                TickMover(_movers[i], deltaTime);
        }

        private void TickMover(GuardMover mover, float deltaTime)
        {
            Vector3 pathStep = mover.IsMoving ? _follower.NextStep(mover, deltaTime) : Vector3.zero;
            Vector3 push = _separation.PushFor(mover, _movers, deltaTime);
            float share = _stepper.Move(mover, pathStep + push, _map, deltaTime);

            if (!mover.IsMoving)
                return;
            if (_follower.HasArrived(mover))
                Arrive(mover);
            else
                WatchForBeingHeldUp(mover, pathStep, share, deltaTime);
        }

        private void Arrive(GuardMover mover)
        {
            mover.Stop();
            _director.Publish(new Arrived(mover.Guard, mover.Position));
        }

        // Held up means the sweep is cutting the guard's own step, not that a crowd is pushing it.
        private void WatchForBeingHeldUp(GuardMover mover, Vector3 pathStep, float share, float deltaTime)
        {
            bool heldUp = pathStep.sqrMagnitude > 0f && share < _tuning.ProgressShare;
            mover.HeldUpSeconds = heldUp ? mover.HeldUpSeconds + deltaTime : 0f;
            if (mover.HeldUpSeconds >= _tuning.BlockedSeconds)
                RaiseBlocked(mover, BlockedReason.Obstacle);
        }

        // A door closing or opening can make every cached route wrong, and a guard mid-route may now
        // be walking at a shut door, so everyone on the move plans again from where they stand.
        private void ReplanIfDoorsChanged()
        {
            if (_map == null || _map.Version == _planVersion)
                return;
            _planVersion = _map.Version;
            _planner.Clear();
            for (int i = 0; i < _movers.Count; i++)
            {
                if (_movers[i].IsMoving)
                    Plan(_movers[i]);
            }
        }

        private void RaiseBlocked(GuardMover mover, BlockedReason reason)
        {
            mover.Stop();
            _director.Publish(new Blocked(mover.Guard, mover.Position, reason));
        }
    }
}

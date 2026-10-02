using System;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The guard's only way to move. It asks the director's navigation service to walk it somewhere
    /// (a <see cref="MoveRequest"/> on the bus) and turns the answers (<see cref="PathReady"/>,
    /// <see cref="Arrived"/>, <see cref="Blocked"/>) into events a state can use. It never touches the
    /// transform, so there is one mover and no second one to fight it.
    /// </summary>
    public sealed class GuardNavigator
    {
        private readonly Component _guard;
        private readonly GuardTuning _tuning;
        private EnemyDirector _director;

        /// <summary>A route exists and the guard has started along it.</summary>
        public event Action<PathReady> RouteReady;

        /// <summary>The guard reached the destination.</summary>
        public event Action<Arrived> Reached;

        /// <summary>The guard cannot get to the destination.</summary>
        public event Action<Blocked> RouteBlocked;

        public GuardNavigator(Component guard, GuardTuning tuning)
        {
            _guard = guard;
            _tuning = tuning;
        }

        /// <summary>Where the guard was last asked to go, or null when it is standing.</summary>
        public Vector3? Destination { get; private set; }

        public bool IsMoving => Destination.HasValue;

        /// <summary>Starts listening to <paramref name="director"/> and gives its navigation service a mover for this guard.</summary>
        public void Attach(EnemyDirector director)
        {
            Detach();
            _director = director;
            if (director == null)
                return;

            director.OnPathReady += HandlePathReady;
            director.OnArrived += HandleArrived;
            director.OnBlocked += HandleBlocked;
            director.Navigation.Register(_guard, _tuning.BodyRadius, _tuning.BodyHeight);
        }

        public void Detach()
        {
            if (_director == null)
                return;

            _director.OnPathReady -= HandlePathReady;
            _director.OnArrived -= HandleArrived;
            _director.OnBlocked -= HandleBlocked;
            _director.Navigation.Unregister(_guard);
            _director = null;
            Destination = null;
        }

        /// <summary>Asks the navigation service to walk the guard to <paramref name="destination"/>. Replaces any move in progress.</summary>
        public void MoveTo(Vector3 destination, float speed, MoveReason reason)
        {
            if (_director == null)
                return;

            Destination = destination;
            _director.Publish(new MoveRequest(_guard, destination, speed, reason));
        }

        /// <summary>Stops the guard where it stands. No event follows: the caller asked for it.</summary>
        public void Stop()
        {
            Destination = null;
            _director?.Navigation.Cancel(_guard);
        }

        /// <summary>Hands the guard's position to something else (Levo's lift) by stopping the navigation service from moving it.</summary>
        public void Pause() => _director?.Navigation.SetPaused(_guard, true);

        /// <summary>Takes the position back once whatever else was moving the guard is done.</summary>
        public void Resume() => _director?.Navigation.SetPaused(_guard, false);

        private void HandlePathReady(PathReady answer)
        {
            if (answer.Guard == _guard)
                RouteReady?.Invoke(answer);
        }

        private void HandleArrived(Arrived answer)
        {
            if (answer.Guard != _guard)
                return;

            Destination = null;
            Reached?.Invoke(answer);
        }

        private void HandleBlocked(Blocked answer)
        {
            if (answer.Guard != _guard)
                return;

            Destination = null;
            RouteBlocked?.Invoke(answer);
        }
    }
}

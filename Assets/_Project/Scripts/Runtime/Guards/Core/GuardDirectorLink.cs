using System;
using Plunderspell.Alarm;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The guard's tie to the enemy director: it registers the guard so the director knows it exists,
    /// and listens for the requests the director relays. Guards never call each other; everything a
    /// guard learns about the rest of the castle comes through here as an event, and the current state
    /// decides what to do with it.
    /// </summary>
    public sealed class GuardDirectorLink
    {
        /// <summary>A request that is not the hue and cry is taken up only by guards this close to its spot.</summary>
        public const float HueAndCryRadius = 40f;

        private readonly Component _guard;
        private int _requestFrame = -1;
        private float _requestDistance;

        /// <summary>Raised with the spot to look at when the director asks guards to investigate and this one is in range.</summary>
        public event Action<Vector3> InvestigateRequested;

        /// <summary>Raised with the player when another guard within <see cref="HueAndCryRadius"/> cannot reach
        /// them and asks for help (#237).</summary>
        public event Action<Transform> HelpCalled;

        public GuardDirectorLink(Component guard)
        {
            _guard = guard;
        }

        /// <summary>The director this guard reports to, or null outside a raid.</summary>
        public EnemyDirector Director { get; private set; }

        /// <summary>The castle alert level, or Calm with no director to ask.</summary>
        public AlarmState Alarm => Director != null ? Director.State : AlarmState.Calm;

        /// <summary>Number of guard requests received while in range, for tests.</summary>
        public int RequestsAccepted { get; private set; }

        public void Attach(EnemyDirector director)
        {
            Detach();
            Director = director;
            if (director == null)
                return;

            director.OnInvestigateRequest += HandleInvestigateRequest;
            director.OnUnreachableIntruder += HandleUnreachableIntruder;
            director.RegisterGuard(_guard);
        }

        public void Detach()
        {
            if (Director == null)
                return;

            Director.OnInvestigateRequest -= HandleInvestigateRequest;
            Director.OnUnreachableIntruder -= HandleUnreachableIntruder;
            Director.UnregisterGuard(_guard);
            Director = null;
        }

        private void HandleUnreachableIntruder(UnreachableIntruderReported call)
        {
            if (call.Guard == _guard || call.Guard == null || call.Intruder == null)
                return;
            if (Vector3.Distance(_guard.transform.position, call.Guard.transform.position) <= HueAndCryRadius)
                HelpCalled?.Invoke(call.Intruder);
        }

        // When several players are asked about in one frame the guard keeps the nearest.
        private void HandleInvestigateRequest(InvestigateRequest request)
        {
            float distance = Vector3.Distance(_guard.transform.position, request.Position);
            // Every guard in the castle answers the hue and cry, wherever it is (#239).
            if (request.Reason != InvestigateReason.HueAndCry && distance > HueAndCryRadius)
                return;
            if (_requestFrame == Time.frameCount && distance >= _requestDistance)
                return;

            _requestFrame = Time.frameCount;
            _requestDistance = distance;
            RequestsAccepted++;
            InvestigateRequested?.Invoke(request.Position);
        }
    }
}

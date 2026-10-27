using Interfaces;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The Lantern Warden turning into the Wick (#417). The guard stands still for the whole turn while
    /// every peer plays it (<see cref="GuardTurnView"/>, from the replicated turned flag). On entry it
    /// becomes the Wick for the rest of the raid: Wick speeds, sees only inside its own fire's light, a
    /// catch that kills. When the turn ends it hunts the player it saw, or goes to look where it was hit
    /// or woken. Nothing returns to human form.
    /// </summary>
    public sealed class TurningState : GuardState
    {
        private Transform _target;
        private Vector3 _spot;
        private float _secondsLeft;

        public TurningState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Turning;

        /// <summary>Names who it saw (or null) and where to look afterwards. Call before entering.</summary>
        public void Begin(Transform target, Vector3 spot)
        {
            _target = target;
            _spot = spot;
        }

        public override void Enter()
        {
            Context.Navigator.Stop();
            Context.Link.Director?.AttackTurns.Release(Context);
            _secondsLeft = Context.Tuning.TrueForm.TurnSeconds;
            Context.Tuning.TrueForm.BecomeWick(Context.Tuning);
            Context.Sight.SeeOnlyInLight(Context.Tuning.TrueForm.LightRadius);
            Context.MarkTurned();
        }

        public override State<Guard> Tick(float deltaTime)
        {
            _secondsLeft -= deltaTime;
            if (_secondsLeft > 0f)
                return this;

            if (_target != null && !Downable.IsDown(_target))
            {
                Context.States.Chase.Follow(_target, true);
                return Context.States.Chase;
            }
            Context.Leads.Offer(_spot, GuardLeads.SightingStrength);
            return Context.States.Investigate;
        }
    }
}

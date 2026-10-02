using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Frango and Levo (#211). A stunned guard stands until the stun runs out. A levitated guard counts as
    /// stunned until it lands (owner's answer 2), however long the fall takes.
    ///
    /// While levitated the guard's position is the spell's, so the state pauses the guard's mover in the
    /// navigation service (<see cref="GuardNavigator.Pause"/>): the service neither steps it, separates it
    /// from other guards nor settles its height onto the floor. It resumes the mover on landing, or on
    /// leaving the state for any other reason (death), so a mover is never left paused.
    /// </summary>
    public sealed class StunnedState : IncapacitatedState
    {
        private readonly GuardLanding _landing;
        private bool _airborne;

        public StunnedState(Guard guard) : base(guard)
        {
            _landing = new GuardLanding(guard);
        }

        public override void Enter()
        {
            base.Enter();
            _airborne = false;
            NoteLevitation();
        }

        public override void Exit() => TouchDown();

        public override State<Guard> Tick(float deltaTime)
        {
            NoteLevitation();
            if (_airborne && !_landing.HasLanded)
                return this;

            TouchDown();
            if (Context.Status.IsStunned)
                return this;

            return Context.Status.IsAsleep ? Context.States.Slept : Recover();
        }

        // Pause once, on the first sight of the spell, so a second Levo on a falling guard does not pause twice.
        private void NoteLevitation()
        {
            if (_airborne || !Context.Status.IsLevitating)
                return;

            _airborne = true;
            Context.Navigator.Pause();
        }

        private void TouchDown()
        {
            if (!_airborne)
                return;

            _airborne = false;
            Context.Navigator.Resume();
        }
    }
}

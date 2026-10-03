using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Somnus (#211): the guard lies where it is until the sleep runs out. A loud noise wakes it early
    /// (owner's answer 3): <see cref="GuardHearing"/> does the waking and also leaves the noise as a lead,
    /// so the guard wakes up and goes to look at it. A stun or levitation landing on a sleeper hands over
    /// to <see cref="StunnedState"/>, which holds it for longer.
    /// </summary>
    public sealed class SleptState : IncapacitatedState
    {
        public SleptState(Guard guard) : base(guard)
        {
        }

        public override State<Guard> Tick(float deltaTime)
        {
            if (Context.Status.IsStunned || Context.Status.IsLevitating)
                return Context.States.Stunned;

            return Context.Status.IsAsleep ? this : Recover();
        }
    }
}

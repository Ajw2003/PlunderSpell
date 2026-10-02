using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// What Stunned and Slept (#211) share: the guard stands still, drops its walk and its attack turn on
    /// entry, and when the status ends picks where to go next. The status receiver owns the timers, so
    /// "stands still for a set time" is the status running out; the state only waits for it.
    ///
    /// Two states rather than one because they differ in what ends them: a sleeper can be woken by noise,
    /// and a levitated guard is held until it lands. Each reads clearly on its own.
    /// </summary>
    public abstract class IncapacitatedState : GuardState
    {
        protected IncapacitatedState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Incapacitated;

        public override void Enter()
        {
            Context.Navigator.Stop();
            // A guard that hit the ground mid-fight must not keep blocking the next guard's strike.
            Context.Link.Director?.AttackTurns.Release(Context);
        }

        /// <summary>
        /// Where to go once nothing holds the guard. A noise that woke it (or was heard while it lay there)
        /// is a lead, so it goes to look. With no lead it still goes to look around where it stands once
        /// the castle is Roused or worse, and otherwise it goes back to its round (<see cref="GuardRecovery"/>).
        /// A guard still on fire when the hold ends goes on to panic (#212): stun and sleep outrank burning.
        /// </summary>
        protected State<Guard> Recover()
        {
            if (Context.Status.IsBurning)
                return Context.States.OnFire;

            return GuardRecovery.PatrolOrInvestigate(Context);
        }
    }
}

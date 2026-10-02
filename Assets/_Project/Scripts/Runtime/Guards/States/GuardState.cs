using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// A guard state. Adds to the generic state one thing the guard needs from every state: which
    /// replicated <see cref="GuardAlertState"/> clients, audio and the HUD should see while it is
    /// active. A state decides its own exits by returning the next state from Tick, so the guard
    /// itself holds no transition logic.
    /// </summary>
    public abstract class GuardState : State<Guard>
    {
        protected GuardState(Guard guard) : base(guard)
        {
        }

        /// <summary>The value written to the replicated state while this state is active.</summary>
        public abstract GuardAlertState AlertState { get; }
    }
}

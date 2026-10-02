using System;

namespace StateMachine
{
    /// <summary>
    /// A plain C# state machine, not a MonoBehaviour, so a networked object (which already has a
    /// base class) can own one. Changing state runs Exit on the old state, then Enter on the new.
    /// </summary>
    /// <typeparam name="TContext">The thing the states act on.</typeparam>
    public class StateMachine<TContext>
    {
        /// <summary>The active state, or null before the first ChangeState. Exposed for tests and replication.</summary>
        public State<TContext> Current { get; private set; }

        /// <summary>Raised after a change, with the old state (null the first time) and the new one.</summary>
        public event Action<State<TContext>, State<TContext>> StateChanged;

        /// <summary>Switches state: Exit on the current one, then Enter on the next. Does nothing for the same state.</summary>
        public void ChangeState(State<TContext> next)
        {
            if (next == null || next == Current)
                return;

            State<TContext> previous = Current;
            previous?.Exit();
            Current = next;
            next.Enter();
            StateChanged?.Invoke(previous, next);
        }

        /// <summary>Ticks the current state and follows the state it returns.</summary>
        public void Tick(float deltaTime)
        {
            if (Current == null)
                return;

            State<TContext> next = Current.Tick(deltaTime);
            if (next != null && next != Current)
                ChangeState(next);
        }

        /// <summary>Runs the current state's physics step.</summary>
        public void FixedTick(float deltaTime)
        {
            Current?.FixedTick(deltaTime);
        }
    }
}

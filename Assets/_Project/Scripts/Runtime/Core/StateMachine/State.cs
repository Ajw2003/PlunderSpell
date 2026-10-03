namespace StateMachine
{
    /// <summary>
    /// A state for <see cref="StateMachine{TContext}"/>. Each state decides its own next state, so
    /// transitions live in the states and not in a switch somewhere else.
    /// </summary>
    /// <typeparam name="TContext">The thing the states act on, such as a guard.</typeparam>
    public abstract class State<TContext>
    {
        /// <summary>The object this state acts on.</summary>
        protected readonly TContext Context;

        /// <summary>Creates a state bound to its context.</summary>
        protected State(TContext context)
        {
            Context = context;
        }

        /// <summary>Runs once when the machine switches to this state.</summary>
        public virtual void Enter()
        {
        }

        /// <summary>Runs every frame. Returns the state to switch to, or this state to stay.</summary>
        public virtual State<TContext> Tick(float deltaTime)
        {
            return this;
        }

        /// <summary>Runs every physics step.</summary>
        public virtual void FixedTick(float deltaTime)
        {
        }

        /// <summary>Runs once when the machine leaves this state, so it can undo what it set up.</summary>
        public virtual void Exit()
        {
        }
    }
}

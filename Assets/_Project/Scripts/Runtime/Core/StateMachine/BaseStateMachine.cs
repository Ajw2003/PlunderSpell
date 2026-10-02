using UnityEngine;

namespace StateMachine
{
    public abstract class BaseStateMachine : MonoBehaviour
    {
        protected IState CurrentState { get; set; }
        protected string CurrentStateName;

        public virtual void ChangeState(IState newState)
        {
            if (newState == CurrentState)
                return;

            // Leave the old state first so it can undo what Enter or Update set up.
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState?.Enter();
            CurrentStateName = CurrentState?.ToString();
        }

        public virtual void Update()
        {
            CurrentState?.Update();
        }

        public virtual void FixedUpdate()
        {
            CurrentState?.FixedUpdate();
        }
    }
}

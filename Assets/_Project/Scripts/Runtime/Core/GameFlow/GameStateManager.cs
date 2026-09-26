using System;

namespace Plunderspell.Core
{
    /// <summary>Tracks which screen/mode the game is in and notifies listeners on change.</summary>
    public class GameStateManager
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        public event Action<GameState, GameState> StateChanged;

        public void ChangeState(GameState next)
        {
            if (next == CurrentState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState = next;
            StateChanged?.Invoke(PreviousState, CurrentState);
        }
    }
}

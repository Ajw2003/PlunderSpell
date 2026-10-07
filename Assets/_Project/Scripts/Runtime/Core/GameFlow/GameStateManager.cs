using Code.Scripts.EventSystems;

namespace Plunderspell.Core
{
    /// <summary>Tracks which screen/mode the game is in and notifies listeners on change.</summary>
    public class GameStateManager
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        public void ChangeState(GameState next)
        {
            if (next == CurrentState)
            {
                return;
            }

            PreviousState = CurrentState;
            CurrentState = next;
            EventManager.Instance?.Publish(new GameStateChanged(PreviousState, CurrentState));
        }
    }
}

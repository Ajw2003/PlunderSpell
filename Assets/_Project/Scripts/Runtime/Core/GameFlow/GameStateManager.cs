using Code.Scripts.EventSystems;

namespace Plunderspell.Core
{
    /// <summary>Tracks which screen/mode the game is in and notifies listeners on change.</summary>
    public class GameStateManager
    {
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        public GameState PreviousState { get; private set; } = GameState.MainMenu;

        /// <summary>Where Resume goes: the state the pause menu was opened from (Playing or LairRoom). Settings in between does not change it.</summary>
        public GameState PausedFrom { get; private set; } = GameState.Playing;

        /// <summary>A raid is on screen: playing, or paused from a raid. The raid HUD and its feedback draw only then, never over the Lair.</summary>
        public bool RaidOnScreen =>
            CurrentState == GameState.Playing || (CurrentState == GameState.Paused && PausedFrom == GameState.Playing);

        public void ChangeState(GameState next)
        {
            if (next == CurrentState)
            {
                return;
            }

            if (next == GameState.Paused && CurrentState != GameState.Settings)
            {
                PausedFrom = CurrentState == GameState.LairRoom ? GameState.LairRoom : GameState.Playing;
            }

            PreviousState = CurrentState;
            CurrentState = next;
            EventManager.Instance?.Publish(new GameStateChanged(PreviousState, CurrentState));
        }
    }
}

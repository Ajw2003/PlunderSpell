using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// The one thing that stops the world's clock. While the pause menu (or Settings opened from it)
    /// is up, time and audio freeze — but only on the machine that owns the simulation, the host or
    /// an offline player. A client's pause menu is an overlay: it cannot stop three other players'
    /// game (#107).
    /// </summary>
    public class PausePolicy : MonoBehaviour
    {
        /// <summary>Whether the world should be frozen in this state. Pure, so the rule is testable.</summary>
        public static bool ShouldFreeze(GameState state, GameState previous, bool isSessionAuthority) =>
            isSessionAuthority &&
            (state == GameState.Paused || (state == GameState.Settings && previous == GameState.Paused));

        private void OnEnable()
        {
            if (GameServices.GameState != null)
                GameServices.GameState.StateChanged += OnStateChanged;
            Apply();
        }

        private void OnDisable()
        {
            if (GameServices.GameState != null)
                GameServices.GameState.StateChanged -= OnStateChanged;
            Unfreeze();
        }

        private void OnStateChanged(GameState previous, GameState next) => Apply();

        /// <summary>Re-asserts the clock. Public so tests can drive it.</summary>
        public void Apply()
        {
            GameStateManager states = GameServices.GameState;
            bool freeze = states != null &&
                          ShouldFreeze(states.CurrentState, states.PreviousState, GameServices.IsSessionAuthority());
            if (freeze)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else
            {
                Unfreeze();
            }
        }

        private static void Unfreeze()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}

using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.UI
{
    /// <summary>
    /// The one thing in the game that touches <see cref="Cursor"/>. It follows
    /// <see cref="GameStateManager.StateChanged"/>: locked and hidden while playing, free and visible
    /// in every screen the player is expected to click.
    ///
    /// Why this is the sole owner: docs/6-decisions/Decisions.md, "One owner for the cursor; input gates on the
    /// state variable".
    /// </summary>
    public class CursorLockPolicy : MonoBehaviour
    {
        private bool m_hasFocus = true;

        /// <summary>
        /// Whether the cursor should be captured in this state. Pure, so the rule can be asserted
        /// without a window or a focus event.
        /// </summary>
        public static bool ShouldCapture(GameState state) => state == GameState.Playing;

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
        }

        /// <summary>
        /// Alt-tabbing must give the cursor back, and coming back must only re-capture it if the game
        /// is still being played — otherwise returning to a pause menu leaves it invisible.
        /// </summary>
        private void OnApplicationFocus(bool hasFocus)
        {
            m_hasFocus = hasFocus;
            Apply();
        }

        private void OnStateChanged(GameState previous, GameState next) => Apply();

        /// <summary>Re-asserts the cursor state. Public so tests can drive it without a focus event.</summary>
        public void Apply()
        {
            GameStateManager states = GameServices.GameState;
            bool capture = m_hasFocus && states != null && ShouldCapture(states.CurrentState);

            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
        }

        /// <summary>Test seam: drive the focus-loss path without a real window.</summary>
        public void SetWindowFocused(bool hasFocus) => OnApplicationFocus(hasFocus);
    }
}

using Plunderspell.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Plunderspell.UI
{
    /// <summary>Keyboard shortcuts (Esc = pause, Tab = inventory) that drive the shared GameStateManager.</summary>
    public class GameFlowInput : MonoBehaviour
    {
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var state = GameServices.GameState.CurrentState;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (state == GameState.Playing)
                {
                    GameServices.GameState.ChangeState(GameState.Paused);
                }
                else if (state == GameState.Paused)
                {
                    GameServices.GameState.ChangeState(GameState.Playing);
                }
            }
        }
    }
}

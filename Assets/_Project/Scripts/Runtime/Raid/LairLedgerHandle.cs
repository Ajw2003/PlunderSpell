using Player;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The ledger table: E while looking at it in the Lair room opens today's Lair screen (debt, banked
    /// gold, Age, company). It reads the key itself because nothing on the raid player handles E.
    /// </summary>
    public class LairLedgerHandle : MonoBehaviour
    {
        [Tooltip("How far from the camera the table can be opened, in metres.")]
        [SerializeField] private float _reach = 3f;

        private void Update()
        {
            if (GameInput.Actions.PlayerActions.Interact.WasPressedThisFrame() && IsLookedAt(Camera.main))
                Open();
        }

        /// <summary>Whether <paramref name="eye"/>'s centre ray hits this table within reach.</summary>
        public bool IsLookedAt(Camera eye) => LookTarget.IsLookedAt(eye, transform, _reach);

        public void Open()
        {
            if (GameServices.GameState.CurrentState == GameState.LairRoom)
                GameServices.GameState.ChangeState(GameState.Lair);
        }
    }
}

using Player;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// A player in the Lair who looks at the ledger and presses E tears the left page out (#448). Any player may, host or
    /// client; <see cref="DemandPageKeeper"/> decides, and a page already torn stays as it is.
    /// </summary>
    public class LedgerPageTearer : MonoBehaviour
    {
        [Tooltip("How far from the camera the book can be torn, in metres.")]
        [SerializeField] private float _reach = 3f;
        [SerializeField] private Transform _book;
        [SerializeField] private DemandPageKeeper _keeper;

        // per-frame: reads the E key, which has no event
        private void Update()
        {
            if (GameInput.Actions.PlayerActions.Interact.WasPressedThisFrame() && CanTear())
                _keeper.RequestTear();
        }

        private bool CanTear() =>
            !_keeper.IsTorn
            && GameServices.GameState.CurrentState == GameState.LairRoom
            && LookTarget.IsLookedAt(Camera.main, _book, _reach);
    }
}

using Player;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>A player who looks at the page lying on the floor and presses E (Interact) picks it up (#450); the keeper decides who gets it.</summary>
    public class PagePicker : MonoBehaviour
    {
        [Tooltip("How far from the camera the page can be picked up, in metres.")]
        [SerializeField] private float _reach = 3f;
        [SerializeField] private DemandPageKeeper _keeper;
        [SerializeField] private DroppedPageView _view;

        // per-frame: reads the Interact key (E), which has no event
        private void Update()
        {
            if (GameInput.Actions.PlayerActions.Interact.WasPressedThisFrame() && _keeper.IsLying && _view.Sheet != null
                && LookTarget.IsLookedAt(Camera.main, _view.Sheet, _reach))
                _keeper.RequestPickUp();
        }
    }
}

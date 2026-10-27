using Code.Scripts.EventSystems;
using Player;
using Plunderspell.Core;
using Plunderspell.Loot;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// The holder of the torn page lets it fall where they stand (#450): on Q with empty hands, or on their death.
    /// Q with loot in hand still drops the loot (<see cref="LootInteractor"/>); this runs first, so one press never does both.
    /// The server decides (<see cref="DemandPageKeeper"/>). Lives with the keeper, so it works in the Lair and in a raid.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class PageDropper : MonoBehaviour
    {
        [Tooltip("How far ahead of the feet the page lands, in metres, so the player can look down at it.")]
        [SerializeField] private float _ahead = 0.4f;
        [SerializeField] private DemandPageKeeper _keeper;

        private void OnEnable() => EventManager.Instance?.Subscribe(this, (LocalPlayerDied _) => Drop());
        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        // per-frame: reads the Q key, which has no event
        private void Update()
        {
            PlayerStateMachine player = PlayerStateMachine.Local;
            if (player == null || !GameInput.Actions.PlayerActions.Drop.WasPressedThisFrame())
                return;
            LootInteractor hands = player.GetComponentInChildren<LootInteractor>();
            if (DemandPageRules.QDropsPage(_keeper.LocalPlayerHolds, hands != null && hands.Carried != null))
                Drop();
        }

        private void Drop()
        {
            PlayerStateMachine player = PlayerStateMachine.Local;
            if (player != null && _keeper.LocalPlayerHolds)
                {
                Transform eye = player.CameraTransform != null ? player.CameraTransform : player.transform;
                Vector3 facing = Vector3.ProjectOnPlane(eye.forward, Vector3.up);
                _keeper.RequestDrop(FloorAhead(player), Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg);
            }
        }

        // The floor a little ahead of where the player faces: the body's pivot is not at the feet, so find the ground under that spot.
        private Vector3 FloorAhead(PlayerStateMachine player)
        {
            Transform eye = player.CameraTransform != null ? player.CameraTransform : player.transform;
            Vector3 ahead = Vector3.ProjectOnPlane(eye.forward, Vector3.up).normalized * _ahead;
            Vector3 above = player.transform.position + ahead + Vector3.up;
            Vector3 point = player.transform.position + ahead;
            float nearest = float.MaxValue;
            foreach (RaycastHit hit in Physics.RaycastAll(above, Vector3.down, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.distance < nearest && !hit.collider.transform.IsChildOf(player.transform))
                {
                    nearest = hit.distance;
                    point = hit.point;
                }
            }
            return point;
        }
    }
}

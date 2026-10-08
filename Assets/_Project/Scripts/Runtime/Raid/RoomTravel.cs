using Player;
using Plunderspell.Core;
using PurrNet;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Moves the local player between the Lair and the Market: to <c>Spawn{n}</c> under a destination,
    /// n by owner number, so four players never land inside each other. Used by the Market door
    /// (look and press E) and the Market's way out (walk through it). Only in <see cref="GameState.LairRoom"/>.
    /// </summary>
    public class RoomTravel : MonoBehaviour
    {
        public enum Trigger { LookAndPress, WalkThrough }

        [SerializeField] private Trigger _trigger = Trigger.LookAndPress;
        [Tooltip("Has children Spawn1..Spawn4: where each player lands, facing each spawn's forward.")]
        [SerializeField] private Transform _destination;
        [SerializeField] private float _reach = 3f;

        private void Update()
        {
            if (_trigger == Trigger.LookAndPress && GameInput.Actions.PlayerActions.Interact.WasPressedThisFrame() && LookTarget.IsLookedAt(Camera.main, transform, _reach))
                Travel(PlayerStateMachine.Local);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_trigger != Trigger.WalkThrough)
                return;
            PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
            if (player != null && player == PlayerStateMachine.Local)
                Travel(player);
        }

        public void Travel(PlayerStateMachine player)
        {
            if (player == null || GameServices.GameState.CurrentState != GameState.LairRoom)
                return;
            if (_destination == null)
            {
                Debug.LogWarning($"[RoomTravel] {name} has no destination; the player stays where they are.");
                return;
            }

            int index = player.TryGetComponent(out NetworkIdentity identity) && identity.owner.HasValue
                ? Mathf.Clamp((int)(ulong)identity.owner.Value.id - 1, 0, 3)
                : 0;
            Transform spawn = _destination.Find($"Spawn{index + 1}");
            Item carried = CarriedTravel.Find();
            Transform view = Camera.main != null ? Camera.main.transform : player.transform;
            Vector3 bodyBefore = player.transform.position;
            float yawBefore = view.eulerAngles.y;
            Pose pieceBefore = carried != null ? new Pose(carried.transform.position, carried.transform.rotation) : default;

            PlayerPlacement.StandAt(player, spawn != null ? spawn : _destination);

            if (carried != null)
            {
                float yawDelta = view.eulerAngles.y - yawBefore;
                CarriedTravel.Place(carried, CarriedTravel.Moved(pieceBefore, bodyBefore, player.transform.position, yawDelta));
            }
        }
    }
}

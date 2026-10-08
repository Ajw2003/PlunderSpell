using Code.Scripts.EventSystems;
using PurrNet;
using Plunderspell.Core;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// On arriving in <see cref="GameState.LairRoom"/>, stands the local player at their own spawn in the
    /// Lair room (PlayerSpawns/Spawn{owner}) facing into the room. A session's body may arrive a frame
    /// after the state changes, so the move is retried until a local player exists.
    /// </summary>
    public class LairRoomSpawner : MonoBehaviour
    {
        private bool _pending;

        /// <summary>
        /// Whether entering the room from <paramref name="previous"/> is an arrival (from the menu, a raid,
        /// the death screen) rather than resuming from the pause menu, which leaves the player where they stood.
        /// </summary>
        public static bool IsArrival(GameState previous) => previous != GameState.Paused;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (GameStateChanged e) =>
            {
                if (e.Current == GameState.LairRoom && IsArrival(e.Previous))
                    _pending = true;
            });
        }

        private void OnDisable() => EventManager.Instance?.UnsubscribeFromAllEvents(this);

        private void Update()
        {
            if (_pending && PlayerStateMachine.Local != null)
            {
                _pending = false;
                PlaceLocalPlayer(PlayerStateMachine.Local);
            }
        }

        private void PlaceLocalPlayer(PlayerStateMachine player)
        {
            int index = player.TryGetComponent(out NetworkIdentity identity) && identity.owner.HasValue
                ? Mathf.Max(0, (int)(ulong)identity.owner.Value.id - 1)
                : 0;
            Transform spawns = transform.Find("PlayerSpawns");
            Transform spawn = spawns != null ? spawns.Find($"Spawn{Mathf.Min(index, 3) + 1}") : null;
            if (spawn == null)
            {
                Debug.LogWarning($"[Lair] No spawn point {index + 1} under {name}; the player stays where they are.");
                return;
            }

            RaidDirector.ClearCarriedOverState(player.gameObject);
            PlayerPlacement.StandAt(player, spawn);
        }
    }
}

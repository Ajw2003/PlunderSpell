using Code.Scripts.EventSystems;
using PurrNet;
using Plunderspell.Core;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// On entering <see cref="GameState.LairRoom"/>, stands the local player at their own spawn in the
    /// Lair room (PlayerSpawns/Spawn{owner}) facing into the room. A session's body may arrive a frame
    /// after the state changes, so the move is retried until a local player exists.
    /// </summary>
    public class LairRoomSpawner : MonoBehaviour
    {
        private const float BodyHeightAboveFloor = 1.0f;
        private bool _pending;

        private void OnEnable()
        {
            EventManager.Instance?.Subscribe(this, (GameStateChanged e) =>
            {
                if (e.Current == GameState.LairRoom)
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
            Vector3 position = spawn.position + Vector3.up * BodyHeightAboveFloor;
            player.FaceYaw(spawn.eulerAngles.y);
            if (player.TryGetComponent(out Rigidbody body))
            {
                body.position = position;
                body.linearVelocity = Vector3.zero;
            }
            player.transform.position = position;
            Debug.Log($"[Lair] Placed {player.name} at {player.transform.position} (spawn {index + 1}).");
        }
    }
}

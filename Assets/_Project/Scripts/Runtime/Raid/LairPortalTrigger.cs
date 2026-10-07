using Plunderspell.Core;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// A trigger volume in front of the portal arch: the local player walking in while in the Lair room
    /// sets out. Only the host (or a solo player) may; a friend waits for the host and follows.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LairPortalTrigger : MonoBehaviour
    {
        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (GameServices.GameState.CurrentState != GameState.LairRoom || !GameServices.IsSessionAuthority())
                return;

            PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
            if (player != null && player == PlayerStateMachine.Local)
                GameServices.GameState.ChangeState(GameState.Playing);
        }
    }
}

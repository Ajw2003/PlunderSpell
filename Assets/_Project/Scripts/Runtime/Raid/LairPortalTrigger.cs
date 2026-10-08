using Plunderspell.Core;
using StateMachine;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// A trigger volume in front of the portal arch: the local player walking in while in the Lair room
    /// sets out. Only the host (or a solo player) may; a friend who walks in sees "The host sets out" at the
    /// arch and waits for the host to go, then follows.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class LairPortalTrigger : MonoBehaviour
    {
        private HostSetsOutLine _hostLine;

        public HostSetsOutLine HostLine => _hostLine;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void Awake() => _hostLine = HostSetsOutLine.Create(transform);

        private void OnTriggerEnter(Collider other)
        {
            if (GameServices.GameState.CurrentState != GameState.LairRoom)
                return;

            PlayerStateMachine player = other.GetComponentInParent<PlayerStateMachine>();
            if (player == null || player != PlayerStateMachine.Local)
                return;

            if (GameServices.IsSessionAuthority())
                GameServices.GameState.ChangeState(GameState.Playing);
            else
                _hostLine.Show();
        }
    }
}

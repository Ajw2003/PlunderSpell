using Player;
using PurrNet;
using UnityEngine;

// Gates input/camera/simulation on network ownership for the minimal local-multiplayer test
// (docs/plans/steam-coop-framework.md Phase 3). One physical client always ends up with more
// than one active MainCamera/AudioListener and double-simulated remote bodies unless this runs.
public class PlayerNetworkOwnership : NetworkBehaviour
{
    [SerializeField] private PlayerInputController _inputController;
    [SerializeField] private GameObject _playerCamera;
    [SerializeField] private Rigidbody _rigidbody;

    [Tooltip("Components only the owning machine may run: input, the microphone, anything that " +
             "reads this machine's keyboard or voice. Switched off on every other machine's copy.")]
    [SerializeField] private Behaviour[] _ownerOnly = new Behaviour[0];

    private void Awake()
    {
        if (TryGetComponent(out StateMachine.PlayerStateMachine stateMachine))
            stateMachine.LocalDecidedByNetwork = true;
        if (_inputController == null) _inputController = GetComponent<PlayerInputController>();
        if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
        if (_playerCamera == null)
        {
            var pivot = transform.Find("CameraPivot");
            if (pivot != null)
            {
                var cameraTransform = pivot.Find("PlayerCamera");
                if (cameraTransform != null) _playerCamera = cameraTransform.gameObject;
            }
        }
    }

    // Both callbacks fire twice on a host, once as the server and once as its own client. Ownership
    // is a client-side question ("is this my body?"), so only the client-side call applies it; the
    // server-side one would switch the host's own body off.
    protected override void OnSpawned(bool asServer)
    {
        if (!asServer)
            ApplyOwnership();
    }

    // On a client the spawn arrives before the ownership does, so the body first looks remote and
    // must be switched back on when it turns out to be this machine's.
    protected override void OnOwnerChanged(PlayerID? oldOwner, PlayerID? newOwner, bool asServer)
    {
        if (!asServer)
            ApplyOwnership();
    }

    /// <summary>Only the owner drives input and the camera, and there may be only one MainCamera
    /// and AudioListener per machine. Everyone else's copy follows NetworkTransform, so its
    /// physics would fight the replicated transform and is switched off.</summary>
    private void ApplyOwnership()
    {
        bool mine = isOwner;
        if (_inputController != null) _inputController.enabled = mine;
        foreach (Behaviour behaviour in _ownerOnly)
            if (behaviour != null) behaviour.enabled = mine;
        if (_playerCamera != null) _playerCamera.SetActive(mine);
        if (_rigidbody != null) _rigidbody.isKinematic = !mine;
        if (!TryGetComponent(out StateMachine.PlayerStateMachine body))
            return;
        if (mine)
        {
            body.ClaimLocal();
            Debug.Log($"[Coop] This machine plays as {name} (owner {owner}).");
        }
        else
        {
            body.ReleaseLocal();
        }
    }
}

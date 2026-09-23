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

    protected override void OnSpawned()
    {
        if (isOwner)
            return;

        // Remote players: only the owner drives input/camera, and only one MainCamera/
        // AudioListener may be active per running instance.
        if (_inputController != null) _inputController.enabled = false;
        foreach (Behaviour behaviour in _ownerOnly)
            if (behaviour != null) behaviour.enabled = false;
        if (_playerCamera != null) _playerCamera.SetActive(false);

        // Remote position/rotation follows NetworkTransform instead - simulating physics for
        // it locally would fight the replicated transform.
        if (_rigidbody != null) _rigidbody.isKinematic = true;
    }
}

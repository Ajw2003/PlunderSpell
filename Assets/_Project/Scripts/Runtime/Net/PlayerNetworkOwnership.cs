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

    // Set by the owner when its player goes down, cleared when it is revived. The server watches
    // every body's flag to know when the whole party is down.
    private readonly SyncVar<bool> _isDown = new SyncVar<bool>(false, ownerAuth: true);

    private static readonly System.Collections.Generic.List<PlayerNetworkOwnership> s_bodies =
        new System.Collections.Generic.List<PlayerNetworkOwnership>();

    private Camera _spectatorCamera;
    private PlayerNetworkOwnership _watching;

    /// <summary>True while this player is down (dead, waiting for the raid to end).</summary>
    public bool IsDown => _isDown.value;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    private static void InstallSpectating()
    {
        s_bodies.Clear();
        StateMachine.PlayerStateMachine.SpectateOnDeath = AnyTeammateStanding;
    }

    private static bool AnyTeammateStanding()
    {
        foreach (PlayerNetworkOwnership body in s_bodies)
            if (body != null && body.isSpawned && !body.isOwner && !body.IsDown && !body.IsDeadHere)
                return true;
        return false;
    }

    private bool IsDeadHere => TryGetComponent(out StateMachine.PlayerStateMachine body) && body.dead;

    private void OnEnable() => s_bodies.Add(this);

    private void OnDisable()
    {
        s_bodies.Remove(this);
        StopSpectating();
    }

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
        if (asServer)
            _isDown.onChanged += OnDownChangedOnServer;
        else
            ApplyOwnership();
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer)
            _isDown.onChanged -= OnDownChangedOnServer;
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
        // The camera object stays active on every machine so its NetworkTransform keeps reporting
        // where this player looks (a spectating teammate watches through it); only the camera and
        // its listener are switched off, since a machine may have one of each.
        if (_playerCamera != null)
        {
            if (_playerCamera.TryGetComponent(out Camera view)) view.enabled = mine;
            if (_playerCamera.TryGetComponent(out AudioListener ears)) ears.enabled = mine;
        }
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

    // -----------------------------------------------------------------------------------------
    // Going down, spectating, and the party wipe. See docs/systems/net.md, "Spectating".
    // -----------------------------------------------------------------------------------------

    private void Update()
    {
        if (!isSpawned || !isOwner || !TryGetComponent(out StateMachine.PlayerStateMachine body))
            return;

        if (body.dead && !_isDown.value)
        {
            _isDown.value = true;
            if (AnyTeammateStanding())
                Debug.Log("[Coop] Down. Spectating a teammate until the raid ends.");
        }
        else if (!body.dead && _isDown.value)
        {
            _isDown.value = false;
            StopSpectating();
        }

        if (_isDown.value)
            KeepSpectating();
    }

    private void KeepSpectating()
    {
        if (_watching == null || _watching.IsDown || !_watching.isSpawned)
            _watching = NextTeammateStanding();
        if (_watching == null)
        {
            StopSpectating();
            return;
        }

        if (_spectatorCamera == null)
        {
            var go = new GameObject("SpectatorCamera");
            _spectatorCamera = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
            go.tag = "MainCamera";
            if (_playerCamera != null)
            {
                if (_playerCamera.TryGetComponent(out Camera own)) own.enabled = false;
                if (_playerCamera.TryGetComponent(out AudioListener ownEars)) ownEars.enabled = false;
            }
        }

        Transform eye = _watching._playerCamera != null ? _watching._playerCamera.transform : _watching.transform;
        _spectatorCamera.transform.SetPositionAndRotation(eye.position, eye.rotation);
    }

    private PlayerNetworkOwnership NextTeammateStanding()
    {
        foreach (PlayerNetworkOwnership body in s_bodies)
            if (body != null && body != this && body.isSpawned && !body.IsDown)
                return body;
        return null;
    }

    private void StopSpectating()
    {
        _watching = null;
        if (_spectatorCamera == null)
            return;
        Destroy(_spectatorCamera.gameObject);
        _spectatorCamera = null;
        if (isSpawned && isOwner && _playerCamera != null)
        {
            if (_playerCamera.TryGetComponent(out Camera own)) own.enabled = true;
            if (_playerCamera.TryGetComponent(out AudioListener ownEars)) ownEars.enabled = true;
        }
    }

    private void OnGUI()
    {
        if (_spectatorCamera == null || _watching == null)
            return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperCenter };
        GUI.Label(new Rect(0f, 20f, Screen.width, 40f), "You are down. Watching your teammate until the raid ends.", style);
    }

    /// <summary>On the server: once every player in the session is down, the raid is lost for
    /// everyone.</summary>
    private void OnDownChangedOnServer(bool down)
    {
        if (!down)
            return;
        foreach (PlayerNetworkOwnership body in s_bodies)
            if (body != null && body.isSpawned && !body.IsDown)
                return;
        Debug.Log("[Coop] Every player is down; the raid is lost.");
        PartyDown();
    }

    [ObserversRpc]
    private void PartyDown()
    {
        StopSpectatingEverywhere();
        if (Plunderspell.Core.GameServices.GameState != null)
            Plunderspell.Core.GameServices.GameState.ChangeState(Plunderspell.Core.GameState.GameOver);
    }

    private static void StopSpectatingEverywhere()
    {
        foreach (PlayerNetworkOwnership body in s_bodies)
            if (body != null)
                body.StopSpectating();
    }
}

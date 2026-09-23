#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using Plunderspell.Core;
using PurrNet;
using PurrNet.Transports;
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
using PurrNet.Steam;
using Steamworks;
#endif
using UnityEngine;

namespace RogueAi.Net
{
    /// <summary>
    /// The one place a network session starts and ends. Every session, solo included, runs through
    /// PurrNet's <see cref="NetworkManager"/>, so the raid has a single code path: solo is a host on
    /// <see cref="LocalTransport"/> that nobody else can reach, co-op is a host on Steam (or UDP on a
    /// LAN when Steam is not running), and a friend is a client. See docs/systems/net.md.
    ///
    /// Joining over Steam, from any of the three ways Steam delivers a join:
    /// <list type="bullet">
    /// <item>an invite accepted in the overlay while the game runs (<c>GameLobbyJoinRequested_t</c>),</item>
    /// <item>"Join Game" in the friends list while it runs (<c>GameRichPresenceJoinRequested_t</c>,
    /// carrying the rich presence <c>connect</c> string this class sets), and</item>
    /// <item>either of those while the game is closed, which launches it with
    /// <c>+connect_lobby &lt;id&gt;</c> on the command line.</item>
    /// </list>
    /// Each of them enters the lobby; the lobby's owner is the host, and the client connects to the
    /// owner's Steam ID over Steam's peer-to-peer relay. No IP addresses or ports are involved.
    ///
    /// Local testing without Steam: <c>-coop-host</c> hosts over UDP, and
    /// <c>-coop-join &lt;address&gt;</c> joins one.
    /// </summary>
    public sealed class CoopSession : MonoBehaviour, ICoopSession
    {
        private const string ConnectLobbyArg = "+connect_lobby";
        private const string HostUdpArg = "-coop-host";
        private const string JoinUdpArg = "-coop-join";
        private const int MaxPlayers = 4;

        [SerializeField] private NetworkManager _manager;
        [SerializeField] private LocalTransport _localTransport;
        [SerializeField] private UDPTransport _udpTransport;
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
        [SerializeField] private SteamTransport _steamTransport;
#else
        [SerializeField] private GenericTransport _steamTransport;
#endif

        private string _status = "Not connected.";
        private bool _hostingLobby;

        public string Status => _status;
        public bool IsInSession => _manager != null && (_manager.isServer || _manager.isClient);
        public bool CanInvite => _hostingLobby;
        public event Action Changed;

        private void Awake()
        {
            if (_manager == null)
                _manager = GetComponent<NetworkManager>();
            GameServices.Initialize();
            GameServices.Coop = this;
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(GameServices.Coop, this))
                GameServices.Coop = null;
        }

        private void OnEnable()
        {
            _manager.onClientConnectionState += OnClientConnectionState;
            _manager.onServerConnectionState += OnServerConnectionState;
        }

        private void OnDisable()
        {
            _manager.onClientConnectionState -= OnClientConnectionState;
            _manager.onServerConnectionState -= OnServerConnectionState;
        }

        private void Start()
        {
            RegisterSteamCallbacks();
            HandleCommandLine();
        }

        // -----------------------------------------------------------------------------------------
        // Starting and ending
        // -----------------------------------------------------------------------------------------

        public void PlaySolo()
        {
            if (IsInSession)
                return;
            StartHost(_localTransport, "Playing solo.");
        }

        public void HostCoop()
        {
            if (IsInSession)
                Leave();

            if (SteamBootstrap.IsReady && _steamTransport != null)
            {
                HostSteamLobby();
                return;
            }

            StartHost(_udpTransport, $"Hosting on the local network (Steam unavailable: {SteamBootstrap.Problem})");
            GameServices.GameState.ChangeState(GameState.Lair);
        }

        public void Leave()
        {
            LeaveSteamLobby();
            if (_manager.isClient)
                _manager.StopClient();
            if (_manager.isServer)
                _manager.StopServer();
            SetStatus("Not connected.");
        }

        private void StartHost(GenericTransport transport, string status)
        {
            _manager.transport = transport;
            _manager.StartHost();
            SetStatus(status);
        }

        private void JoinUdp(string address)
        {
            if (IsInSession)
                Leave();
            _udpTransport.address = address;
            _manager.transport = _udpTransport;
            _manager.StartClient();
            SetStatus($"Joining {address} on the local network...");
        }

        private void HandleCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == HostUdpArg)
                {
                    StartHost(_udpTransport, "Hosting on the local network (command line).");
                    GameServices.GameState.ChangeState(GameState.Lair);
                    return;
                }

                if (args[i] == JoinUdpArg && i + 1 < args.Length)
                {
                    JoinUdp(args[i + 1]);
                    return;
                }

                if (args[i] == ConnectLobbyArg && i + 1 < args.Length && ulong.TryParse(args[i + 1], out ulong lobby))
                {
                    JoinSteamLobbyWhenReady(lobby);
                    return;
                }
            }
        }

        private void OnServerConnectionState(ConnectionState state)
        {
            Debug.Log($"[Coop] Server {state}.");
            Changed?.Invoke();
        }

        private void OnClientConnectionState(ConnectionState state)
        {
            Debug.Log($"[Coop] Client {state}.");

            // A client that is not also the server has just joined someone else's session: take it
            // to the Lair, where it waits for the host to set out.
            if (state == ConnectionState.Connected && !_manager.isServer)
            {
                SetStatus("Joined. Waiting for the host to set out.");
                GameServices.GameState.ChangeState(GameState.Lair);
            }
            else if (state == ConnectionState.Disconnected && !_manager.isServer && _status.StartsWith("Join"))
            {
                LeaveSteamLobby();
                SetStatus("Disconnected from the host.");
                GameServices.GameState.ChangeState(GameState.MainMenu);
            }

            Changed?.Invoke();
        }

        private void SetStatus(string status)
        {
            _status = status;
            Debug.Log($"[Coop] {status}");
            Changed?.Invoke();
        }

        // -----------------------------------------------------------------------------------------
        // Steam
        // -----------------------------------------------------------------------------------------

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
        private CallResult<LobbyCreated_t> _lobbyCreated;
        private CallResult<LobbyEnter_t> _lobbyEntered;
        private Callback<GameLobbyJoinRequested_t> _lobbyJoinRequested;
        private Callback<GameRichPresenceJoinRequested_t> _richPresenceJoinRequested;
        private CSteamID _lobby = CSteamID.Nil;

        private void RegisterSteamCallbacks()
        {
            if (!SteamBootstrap.IsReady)
                return;
            _lobbyCreated = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
            _lobbyEntered = CallResult<LobbyEnter_t>.Create(OnLobbyEntered);
            _lobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(r => JoinSteamLobby(r.m_steamIDLobby.m_SteamID));
            _richPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(OnRichPresenceJoinRequested);
        }

        private void HostSteamLobby()
        {
            SetStatus("Creating a Steam lobby...");
            _lobbyCreated.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, MaxPlayers));
        }

        private void OnLobbyCreated(LobbyCreated_t result, bool ioFailure)
        {
            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                SetStatus($"Steam could not create a lobby ({result.m_eResult}).");
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            _hostingLobby = true;
            SteamMatchmaking.SetLobbyData(_lobby, "name", $"{SteamFriends.GetPersonaName()}'s raid");
            SteamFriends.SetRichPresence("connect", $"{ConnectLobbyArg} {_lobby.m_SteamID}");

            StartHost(_steamTransport, "Hosting. Invite a friend from the Lair or with Shift+Tab.");
            GameServices.GameState.ChangeState(GameState.Lair);
        }

        public void InviteFriends()
        {
            if (!_hostingLobby)
                return;
            SteamFriends.ActivateGameOverlayInviteDialog(_lobby);
        }

        private void OnRichPresenceJoinRequested(GameRichPresenceJoinRequested_t request)
        {
            string[] parts = request.m_rgchConnect.Split(' ');
            if (parts.Length == 2 && parts[0] == ConnectLobbyArg && ulong.TryParse(parts[1], out ulong lobby))
                JoinSteamLobby(lobby);
        }

        private void JoinSteamLobbyWhenReady(ulong lobby)
        {
            if (!SteamBootstrap.IsReady)
            {
                SetStatus($"Cannot join the invite: {SteamBootstrap.Problem}");
                return;
            }
            JoinSteamLobby(lobby);
        }

        private void JoinSteamLobby(ulong lobby)
        {
            if (IsInSession)
                Leave();
            SetStatus("Joining your friend's lobby...");
            _lobbyEntered.Set(SteamMatchmaking.JoinLobby(new CSteamID(lobby)));
        }

        private void OnLobbyEntered(LobbyEnter_t result, bool ioFailure)
        {
            if (ioFailure || result.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
            {
                SetStatus("Could not join that lobby. It may be full or closed.");
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            CSteamID host = SteamMatchmaking.GetLobbyOwner(_lobby);
            if (host == SteamUser.GetSteamID())
                return; // our own lobby: the host path already started the server

            _steamTransport.address = host.m_SteamID.ToString();
            _manager.transport = _steamTransport;
            _manager.StartClient();
            SetStatus($"Joining {SteamFriends.GetFriendPersonaName(host)}...");
        }

        private void LeaveSteamLobby()
        {
            _hostingLobby = false;
            if (!SteamBootstrap.IsReady || _lobby == CSteamID.Nil)
                return;
            SteamMatchmaking.LeaveLobby(_lobby);
            SteamFriends.SetRichPresence("connect", null);
            _lobby = CSteamID.Nil;
        }
#else
        private void RegisterSteamCallbacks() { }
        private void HostSteamLobby() => StartHost(_udpTransport, "Hosting on the local network.");
        public void InviteFriends() { }
        private void JoinSteamLobbyWhenReady(ulong lobby) => SetStatus("This platform cannot join Steam invites.");
        private void LeaveSteamLobby() => _hostingLobby = false;
#endif
    }
}

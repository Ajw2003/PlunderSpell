#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections;
using PurrLobby;
using PurrLobby.Providers;
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
using Steamworks;
#endif
using UnityEngine;

// See docs/4-systems/net.md for why this exists as a separate component. Closes PurrLobby's
// cold-launch and rich-presence invite gaps (plan Phase 5).
[RequireComponent(typeof(SteamLobbyProvider))]
public class SteamInviteGateway : MonoBehaviour
{
#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
    private const string ConnectLobbyArg = "+connect_lobby";
    private const string RichPresenceConnectKey = "connect";

    // Bounds how long we poll for Steam to finish initializing before giving up on a cold-launch
    // invite. IsSteamClientAvailable has no "ready" event to wait on instead.
    private const int MaxSteamReadyPollFrames = 300;

    [SerializeField] private SteamLobbyProvider _provider;

    private void Reset()
    {
        _provider = GetComponent<SteamLobbyProvider>();
    }

    private void Awake()
    {
        if (_provider == null) _provider = GetComponent<SteamLobbyProvider>();
    }

    private void OnEnable()
    {
        _provider.OnLobbyUpdated += HandleLobbyUpdated;
        _provider.OnLobbyLeft += HandleLobbyLeft;
    }

    private void OnDisable()
    {
        _provider.OnLobbyUpdated -= HandleLobbyUpdated;
        _provider.OnLobbyLeft -= HandleLobbyLeft;
    }

    private void Start()
    {
        StartCoroutine(JoinLobbyFromCommandLineWhenSteamReady());
    }

    private IEnumerator JoinLobbyFromCommandLineWhenSteamReady()
    {
        if (!TryGetCommandLineLobbyId(out ulong lobbyId)) yield break;

        // See docs/4-systems/net.md#how-it-works for why this polls instead of awaiting an event.
        int attempts = 0;
        while (!_provider.IsSteamClientAvailable && attempts < MaxSteamReadyPollFrames)
        {
            attempts++;
            yield return null;
        }

        if (!_provider.IsSteamClientAvailable) yield break;

        _ = _provider.JoinLobbyAsync(lobbyId.ToString());
    }

    private static bool TryGetCommandLineLobbyId(out ulong lobbyId)
    {
        lobbyId = 0;
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] != ConnectLobbyArg) continue;

            // Guard the flag being the last argument (no value follows) and a malformed,
            // non-numeric id.
            if (i + 1 >= args.Length) return false;
            return ulong.TryParse(args[i + 1], out lobbyId);
        }

        return false;
    }

    private void HandleLobbyUpdated(Lobby lobby)
    {
        if (!lobby.IsValid || string.IsNullOrEmpty(lobby.LobbyId)) return;

        SteamFriends.SetRichPresence(RichPresenceConnectKey, $"{ConnectLobbyArg} {lobby.LobbyId}");
    }

    private void HandleLobbyLeft()
    {
        // A null value clears the key (Steamworks.NET's documented way to unset rich presence),
        // so the friends list stops offering a Join button for a lobby we are no longer in.
        SteamFriends.SetRichPresence(RichPresenceConnectKey, null);
    }
#endif
}

#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
using Steamworks;
#endif
using UnityEngine;

namespace Plunderspell.Net
{
    /// <summary>
    /// Starts Steamworks once, before the first scene loads, pumps its callbacks every frame and
    /// shuts it down on quit. Nothing else in the game initialises Steam: PurrLobby's provider only
    /// does so behind a flag, and no shipping scene contains it. See docs/systems/net.md.
    ///
    /// Steam must be running and signed in. Outside Steam, the App ID comes from
    /// <c>steam_appid.txt</c> next to the executable (the build tool copies it there) or at the
    /// project root in the Editor.
    /// </summary>
    public sealed class SteamBootstrap : MonoBehaviour
    {
        /// <summary>True once Steamworks has initialised. Stays false when Steam is not running.</summary>
        public static bool IsReady { get; private set; }

        /// <summary>Why Steam is not ready, for the menu to show. Empty when ready.</summary>
        public static string Problem { get; private set; } = "Steam has not been started yet.";

        private static SteamBootstrap _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnLaunch()
        {
            if (_instance != null)
                return;
            var host = new GameObject(nameof(SteamBootstrap));
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<SteamBootstrap>();
        }

#if STEAMWORKS_NET_PACKAGE && !DISABLESTEAMWORKS
        private void Awake()
        {
            if (!Packsize.Test() || !DllCheck.Test())
            {
                Fail("The Steamworks library does not match this build.");
                return;
            }

            try
            {
                IsReady = SteamAPI.Init();
            }
            catch (System.DllNotFoundException e)
            {
                Fail($"steam_api64.dll is missing: {e.Message}");
                return;
            }

            if (!IsReady)
            {
                Fail("Steam is not running, or you are not signed in to it.");
                return;
            }

            Problem = string.Empty;
            Debug.Log($"[Steam] Ready. Signed in as {SteamFriends.GetPersonaName()} " +
                      $"({SteamUser.GetSteamID().m_SteamID}), app {SteamUtils.GetAppID().m_AppId}, " +
                      $"overlay {(SteamUtils.IsOverlayEnabled() ? "on" : "off (the game was not started by Steam)")}.");
        }

        private void Update()
        {
            if (IsReady)
                SteamAPI.RunCallbacks();
        }

        private void OnApplicationQuit()
        {
            if (!IsReady)
                return;
            IsReady = false;
            SteamAPI.Shutdown();
        }

        private static void Fail(string problem)
        {
            IsReady = false;
            Problem = problem;
            Debug.LogWarning($"[Steam] Not available: {problem} Co-op over Steam is off; solo play is unaffected.");
        }
#else
        private void Awake() => Problem = "This platform has no Steam support.";
#endif
    }
}

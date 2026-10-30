using System.Collections;
using Code.Scripts.EventSystems;
using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Keeps exactly one of the Lair scene and the raid scene loaded on top of the Bootstrap scene, switched by the host
    /// through PurrNet so every client follows, behind a loading screen on every machine (#536, #549;
    /// docs/4-systems/scene-flow.md). Setting out unloads the Lair and loads a fresh raid scene; a raid ending unloads
    /// the raid scene, with everything in it, and loads the Lair. Lives in the Bootstrap scene.
    /// </summary>
    public sealed class SceneFlow : NetworkBehaviour
    {
        [SerializeField] private string _lairScene = "Lair";
        [SerializeField] private string _raidScene = "RaidScene";

        /// <summary>The Bootstrap scene's flow, or null in a scene that has none (the gym, tests).</summary>
        public static SceneFlow Current { get; private set; }

        private NetworkManager _network;
        private bool _switching;

        /// <summary>SessionState key the co-op launcher sets to load a raid scene variant (the Ophan Gym) in place of RaidScene.</summary>
        public const string RaidSceneOverrideKey = "Plunderspell.RaidSceneOverride";

        private void Awake()
        {
            Current = this;
#if UNITY_EDITOR
            string raidOverride = UnityEditor.SessionState.GetString(RaidSceneOverrideKey, "");
            if (!string.IsNullOrEmpty(raidOverride))
                _raidScene = raidOverride;
#endif
        }

        // Start, not OnEnable: the network manager and the event manager in the same scene are set up in their Awake.
        private void Start()
        {
            _network = NetworkManager.main;
            if (_network != null)
                _network.onServerConnectionState += OnServerState;
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnRaidPhase(e.Phase));
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Current == this)
                Current = null;
            if (_network != null)
                _network.onServerConnectionState -= OnServerState;
            EventManager.Instance?.Unsubscribe<RaidPhaseChanged>(this);
            LoadingScreen.Hide();
        }

        private void OnServerState(ConnectionState state)
        {
            if (state == ConnectionState.Connected)
                StartCoroutine(Switch(null, _lairScene, null));
        }

        /// <summary>Host: leave the Lair for a fresh raid scene, then start the raid in it.</summary>
        public void SetOut()
        {
            if (_network == null || !_network.isServer || _switching)
                return;
            StartCoroutine(Switch(_lairScene, _raidScene, () => RaidDirector.Current?.StartRaid()));
        }

        // A raid ended and the director is back in the Lair: the raid scene goes, with everything in it.
        private void OnRaidPhase(RaidPhase phase)
        {
            if (phase != RaidPhase.InLair || _network == null || !_network.isServer || _switching || !IsLoaded(_raidScene))
                return;
            RaidDirector.Current?.UnbindRaidScene();
            StartCoroutine(Switch(_raidScene, _lairScene, null));
        }

        private IEnumerator Switch(string from, string to, System.Action whenLoaded)
        {
            _switching = true;
            _switchId++;
            _covered = 0;
            // The card goes up in the scene being left, on every machine, before anything unloads. An observers RPC is
            // not run at once on the host (PurrNet's runLocally defaults to false), so the host covers itself directly;
            // the unload waits for every machine to confirm its card (aj, 2026-10-29: a frame of empty scene showed).
            Cover(to, _switchId);
            ShowLoading(to, _switchId);
            float giveUp = Time.unscaledTime + CoverTimeoutSeconds;
            while (_covered < Mathf.Max(1, _network.playerCount) && Time.unscaledTime < giveUp)
                yield return null;
            if (_covered < _network.playerCount)
                Debug.LogWarning($"[SceneFlow] Switching to {to} with {_covered} of {_network.playerCount} loading screens confirmed up.");
            if (from != null && IsLoaded(from))
            {
                AsyncOperation unload = _network.sceneModule.UnloadSceneAsync(from);
                while (unload != null && !unload.isDone)
                    yield return null;
            }
            if (!IsLoaded(to))
            {
                AsyncOperation load = _network.sceneModule.LoadSceneAsync(to, new PurrSceneSettings { mode = LoadSceneMode.Additive, isPublic = true });
                while (load != null && !load.isDone)
                    yield return null;
            }
            whenLoaded?.Invoke();
            _switching = false;
        }

        // Every machine shows the screen and keeps it up until it is ready on that machine: a client builds its own
        // castle from the seed, after its own copy of the scene has loaded (aj, 2026-10-29, #549).
        [ObserversRpc]
        private void ShowLoading(string target, int switchId) => Cover(target, switchId);

        // The host covers itself at once and hears the RPC later too, so one cover per switch.
        private void Cover(string target, int switchId)
        {
            if (switchId == _coveringId)
                return;
            _coveringId = switchId;
            if (_cover != null)
                StopCoroutine(_cover);
            _cover = StartCoroutine(CoverUntilReady(target, switchId));
        }

        private IEnumerator CoverUntilReady(string target, int switchId)
        {
            LoadingScreen.Show();
            yield return Fade(0f, 1f, FadeInSeconds);
            if (isServer)
                _covered++;
            else
                ConfirmCovered(switchId);
            while (!ReadyIn(target))
                yield return null;
            // A moment in the new scene under the card, so the players' placement and its first frames are not seen.
            yield return new WaitForSecondsRealtime(HoldSeconds);
            yield return Fade(1f, 0f, FadeOutSeconds);
            LoadingScreen.Hide();
            _cover = null;
        }

        private static IEnumerator Fade(float from, float to, float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                LoadingScreen.Alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            LoadingScreen.Alpha = to;
        }

        [ServerRpc(requireOwnership: false)]
        private void ConfirmCovered(int switchId)
        {
            if (switchId == _switchId)
                _covered++;
        }

        private const float FadeInSeconds = 0.2f;
        private const float HoldSeconds = 0.4f;
        private const float FadeOutSeconds = 0.4f;
        private const float CoverTimeoutSeconds = 3f;
        private int _switchId;
        private int _coveringId = -1;
        private int _covered;
        private Coroutine _cover;

        // In the raid: this machine's castle is built for the raid that started. In the Lair: the Lair scene is loaded
        // and the game is in the Lair.
        private bool ReadyIn(string target)
        {
            if (!IsLoaded(target))
                return false;
            if (target == _raidScene)
            {
                RaidDirector director = RaidDirector.Current;
                return director != null && director.Phase == RaidPhase.Raiding && director.Castle != null;
            }
            return Core.GameServices.GameState == null || Core.GameServices.GameState.CurrentState != Core.GameState.Playing;
        }

        private static bool IsLoaded(string scene) => SceneManager.GetSceneByName(scene).isLoaded;
    }
}

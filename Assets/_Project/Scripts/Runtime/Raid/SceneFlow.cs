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
    /// Loads the Lair scene and the raid scene on top of the Bootstrap scene, on the host, through PurrNet so every
    /// client follows (scene split, #538-#540; docs/4-systems/scene-flow.md). The raid scene is loaded in the background
    /// while players are in the Lair, and unloaded and loaded fresh each time a raid ends, so nothing a raid spawned
    /// outlives it. Lives in the Bootstrap scene.
    /// </summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        [SerializeField] private string _lairScene = "Lair";
        [SerializeField] private string _raidScene = "RaidScene";

        private NetworkManager _network;
        private bool _reloading;

        // Start, not OnEnable: the network manager and the event manager in the same scene are set up in their Awake.
        private void Start()
        {
            _network = NetworkManager.main;
            if (_network != null)
                _network.onServerConnectionState += OnServerState;
            EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnRaidPhase(e.Phase));
        }

        private void OnDestroy()
        {
            if (_network != null)
                _network.onServerConnectionState -= OnServerState;
            EventManager.Instance?.Unsubscribe<RaidPhaseChanged>(this);
        }

        private void OnServerState(ConnectionState state)
        {
            if (state != ConnectionState.Connected)
                return;
            Load(_lairScene);
            Load(_raidScene);
        }

        // Back in the Lair after a raid: the raid scene goes, with everything in it, and a fresh one loads behind it.
        private void OnRaidPhase(RaidPhase phase)
        {
            if (phase != RaidPhase.InLair || _network == null || !_network.isServer || _reloading || !IsLoaded(_raidScene))
                return;
            StartCoroutine(ReloadRaid());
        }

        private IEnumerator ReloadRaid()
        {
            _reloading = true;
            RaidDirector.Current?.UnbindRaidScene();
            AsyncOperation unload = _network.sceneModule.UnloadSceneAsync(_raidScene);
            while (unload != null && !unload.isDone)
                yield return null;
            Load(_raidScene);
            _reloading = false;
        }

        private void Load(string scene)
        {
            if (IsLoaded(scene))
                return;
            _network.sceneModule.LoadSceneAsync(scene, new PurrSceneSettings { mode = LoadSceneMode.Additive, isPublic = true });
        }

        private static bool IsLoaded(string scene) => SceneManager.GetSceneByName(scene).isLoaded;
    }
}

using Player;
using Code.Scripts.EventSystems;
using Plunderspell.Core;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// Starts the loop when the scene runs, and gives a solo playtester the two controls the loop
    /// needs that nothing else provides: call the extraction, and go again.
    ///
    /// This is the seam between "a pile of working systems" and "a thing you can press play on".
    /// It is intentionally thin — every decision it makes belongs to <see cref="RaidDirector"/>; this
    /// only decides when to ask.
    /// </summary>
    [RequireComponent(typeof(RaidDirector))]
    public class RaidBootstrapper : MonoBehaviour
    {
        [Header("Start")]
        [Tooltip("Begin a raid as soon as the scene runs, ignoring the menu and the lair. Off by " +
                 "default: the raid starts when the player sets out, so the menu is reachable.")]
        [SerializeField] private bool _autoStart = false;

        [Tooltip("Era the auto-started raid is set in.")]
        [SerializeField] private HistoricalEra _era = HistoricalEra.HighMedieval;

        [Tooltip("Seconds to wait before auto-starting, so other components finish waking up.")]
        [SerializeField] private float _startDelay = 0.25f;

        private RaidDirector _director;
        private float _elapsed;
        private bool _started;

        private void Awake() => _director = GetComponent<RaidDirector>();

        private void OnEnable()
        {
            // UIBootstrapper initialises the services on AfterSceneLoad, which runs *after* this
            // OnEnable. Initialize() is idempotent, so calling it here removes the order dependency
            // rather than relying on one.
            GameServices.Initialize();

            EventManager.Instance?.Subscribe(this, (GameStateChanged e) => OnGameStateChanged(e.Previous, e.Current));
            if (_director != null)
            {
                EventManager.Instance?.Subscribe(this, (RaidResolved e) => OnRaidResolved(e.Worth, e.Saved));
                EventManager.Instance?.Subscribe(this, (RaidPhaseChanged e) => OnPhaseChanged(e.Phase));
            }

            // Offline the director is its own authority; networked, only the host may freeze time.
            GameServices.IsSessionAuthority = () => _director == null || !_director.isSpawned || _director.isServer;
        }

        private void OnDisable()
        {
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
            GameServices.IsSessionAuthority = () => true;
        }

        /// <summary>
        /// A client does not choose when to set out: the host does, and the client follows it from
        /// the Lair into the raid once the castle has been built from the replicated seed.
        /// </summary>
        private void OnPhaseChanged(RaidPhase phase)
        {
            bool isClient = _director.isSpawned && !_director.isServer;
            // From the "You died" screen as well as the Lair: after a party wipe the host may set out
            // again before a friend has clicked through to the Lair.
            GameState current = GameServices.GameState.CurrentState;
            if (isClient && phase == RaidPhase.Raiding && (current == GameState.Lair || current == GameState.LairRoom || current == GameState.GameOver))
                GameServices.GameState.ChangeState(GameState.Playing);
        }

        /// <summary>Back to the lair once the takings are counted, so the debt can be paid down.</summary>
        private void OnRaidResolved(float worthExtracted, int playersSaved)
        {
            // A lost raid leaves the "You died" screen up; its button goes to the Lair.
            if (GameServices.GameState.CurrentState == GameState.GameOver)
                return;
            GameServices.GameState.ChangeState(GameState.LairRoom);
        }

        /// <summary>
        /// Sets out when the player leaves the lair, and returns them to it when the raid resolves.
        /// The era comes from the lair rather than this component, so whatever they picked is what
        /// they raid in.
        /// </summary>
        private void OnGameStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.GameOver)
            {
                _director.AbandonRaid();
                return;
            }

            if (next != GameState.Playing || previous == GameState.Paused)
                return;

            if (_director.Phase == RaidPhase.Resolved)
                _director.ReturnToLair();

            if (_director.Phase == RaidPhase.InLair)
                _director.StartRaid();
        }

        private void Update()
        {
            if (_autoStart && !_started)
            {
                _elapsed += Time.deltaTime;
                if (_elapsed >= _startDelay)
                {
                    _started = true;
                    _director.StartRaid(_era);
                }
            }

            if (GameInput.Actions.Debug.CallExtraction.WasPressedThisFrame())
                _director.CallExtraction();

            if (GameInput.Actions.Debug.NextRaid.WasPressedThisFrame())
                StartNextRaid();
        }

        /// <summary>
        /// Returns to the Lair and sets out again. Public so a menu button can call it — the
        /// keyboard shortcut is for playtesting, not the shipping control.
        /// </summary>
        public void StartNextRaid()
        {
            if (_director.Phase == RaidPhase.Resolved)
                _director.ReturnToLair();

            _director.StartRaid(_era);
        }

        /// <summary>Configures the bootstrapper from code, for tooling-built scenes.</summary>
        public void Configure(bool autoStart, HistoricalEra era)
        {
            _autoStart = autoStart;
            _era = era;
        }
    }
}

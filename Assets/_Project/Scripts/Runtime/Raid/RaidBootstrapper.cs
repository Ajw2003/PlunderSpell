using RogueAi.Inventory;
using UnityEngine;

namespace RogueAi.Raid
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
        [Tooltip("Begin a raid as soon as the scene runs, rather than waiting in the Lair.")]
        [SerializeField] private bool _autoStart = true;

        [Tooltip("Era the auto-started raid is set in.")]
        [SerializeField] private HistoricalEra _era = HistoricalEra.HighMedieval;

        [Tooltip("Seconds to wait before auto-starting, so other components finish waking up.")]
        [SerializeField] private float _startDelay = 0.25f;

        [Header("Playtest keys")]
        [Tooltip("Calls the extraction — leave now with whatever is inside the zone.")]
        [SerializeField] private KeyCode _callExtractionKey = KeyCode.F5;

        [Tooltip("Returns to the Lair after a raid resolves, then starts the next one.")]
        [SerializeField] private KeyCode _nextRaidKey = KeyCode.F6;

        private RaidDirector _director;
        private float _elapsed;
        private bool _started;

        private void Awake() => _director = GetComponent<RaidDirector>();

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

            if (Input.GetKeyDown(_callExtractionKey))
                _director.CallExtraction();

            if (Input.GetKeyDown(_nextRaidKey))
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

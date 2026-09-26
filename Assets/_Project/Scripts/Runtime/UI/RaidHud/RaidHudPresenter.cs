using RogueAi.Alarm;
using RogueAi.Extraction;
using RogueAi.Lair;
using RogueAi.Loot;
using RogueAi.Raid;
using RogueAi.Spells;
using UnityEngine;

namespace RogueAi.UI
{
    /// <summary>
    /// Gathers the raid's state into a <see cref="RaidHudModel"/> each frame. Deliberately separate
    /// from any view: what the player is told is logic and gets tested; how it is drawn is not.
    ///
    /// Every reference is optional. A scene with only a raid director and an alarm still produces a
    /// usable HUD, which is what lets the game be played before the UI is authored.
    /// </summary>
    public class RaidHudPresenter : MonoBehaviour
    {
        [Header("Sources (all optional)")]
        [SerializeField] private RaidDirector _director;
        [SerializeField] private ExtractionZone _extractionZone;
        [SerializeField] private AlarmFSMManager _alarm;
        [SerializeField] private LairHubManager _lair;
        [SerializeField] private LootInteractor _interactor;

        [Header("Cast feed")]
        [Tooltip("Seconds the most recent cast stays on screen.")]
        [SerializeField] private float _castLineDuration = 4f;

        private string _lastCastLine = string.Empty;
        private float _lastCastAt = float.NegativeInfinity;

        /// <summary>The model as of the last <see cref="Build"/>.</summary>
        public RaidHudModel Model { get; private set; }

        private void Awake() => AutoWire();

        private void OnEnable() => SpellCastingSystem.CastResolved += OnCastResolved;

        private void OnDisable() => SpellCastingSystem.CastResolved -= OnCastResolved;

        private void Update() => Model = Build();

        /// <summary>Collects the current state. Public so tests call it directly.</summary>
        public RaidHudModel Build()
        {
            LootPickup carried = _interactor != null ? _interactor.Carried : null;
            string carriedName = carried != null && carried.Data != null
                ? carried.Data.DisplayName
                : string.Empty;

            bool stale = Time.time - _lastCastAt > _castLineDuration;

            return new RaidHudModel(
                _director != null ? _director.Phase : RaidPhase.InLair,
                _extractionZone != null ? _extractionZone.TimeRemaining : 0f,
                _alarm != null ? _alarm.State : AlarmState.Calm,
                _alarm != null ? _alarm.AlarmLevel : 0f,
                carriedName,
                carried != null && carried.Data != null && carried.Data.RequiresDualCarry,
                BuildInteractPrompt(carried),
                _lair != null ? _lair.TotalDebt : 0f,
                _lair != null ? _lair.AccumulatedGold : 0f,
                stale ? string.Empty : _lastCastLine);
        }

        /// <summary>
        /// The prompt under the crosshair. The "needs two" case is the one that has to be obvious:
        /// a player who does not know an item is a two-person lift will stand there pressing E.
        /// </summary>
        private string BuildInteractPrompt(LootPickup carried)
        {
            if (_interactor == null)
                return string.Empty;

            if (_interactor.FocusDoor != null)
                return "[E] Open";

            LootPickup focus = _interactor.Focus;
            if (focus == null)
                return carried != null ? "[Q] Drop" : string.Empty;

            if (focus.IsBroken)
                return "Broken — worthless";

            if (focus.Data != null && focus.Data.RequiresDualCarry)
            {
                return focus.IsBeingCarried
                    ? "[E] Take the other end"
                    : "[E] Lift — needs two";
            }

            return focus.IsBeingCarried ? string.Empty : "[E] Take";
        }

        private void OnCastResolved(SpellCastingSystem.CastReport report)
        {
            _lastCastLine = report.IsMisfire
                ? $"MISFIRE — {report.Spell}"
                : $"{report.Spell} ({report.Affected} affected)";
            _lastCastAt = Time.time;
        }

        /// <summary>Finds whatever is in the scene. Called on Awake and by tooling-built scenes.</summary>
        public void AutoWire()
        {
            if (_director == null) _director = FindObjectOfType<RaidDirector>();
            if (_extractionZone == null) _extractionZone = FindObjectOfType<ExtractionZone>();
            if (_alarm == null) _alarm = FindObjectOfType<AlarmFSMManager>();
            if (_lair == null) _lair = FindObjectOfType<LairHubManager>();
            if (_interactor == null) _interactor = FindObjectOfType<LootInteractor>();
        }

        /// <summary>Wires the presenter from code, for tests and tooling-built scenes.</summary>
        public void Configure(RaidDirector director, ExtractionZone zone, AlarmFSMManager alarm,
            LairHubManager lair, LootInteractor interactor)
        {
            _director = director;
            _extractionZone = zone;
            _alarm = alarm;
            _lair = lair;
            _interactor = interactor;
        }
    }
}

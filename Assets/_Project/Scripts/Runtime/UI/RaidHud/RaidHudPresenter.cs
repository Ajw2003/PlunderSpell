using RogueAi.Alarm;
using RogueAi.Extraction;
using RogueAi.Guards;
using RogueAi.Lair;
using RogueAi.Loot;
using RogueAi.Raid;
using RogueAi.Spells;
using StateMachine;
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
        [SerializeField] private PlayerStateMachine _player;

        [Header("Cast feed")]
        [Tooltip("Seconds the most recent cast stays on screen.")]
        [SerializeField] private float _castLineDuration = 4f;

        [Tooltip("Height above a guard's pivot to draw its in-world health bar, in metres.")]
        [SerializeField] private float _enemyBarHeight = 2.2f;

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
            bool stale = Time.time - _lastCastAt > _castLineDuration;

            return new RaidHudModel(
                _director != null ? _director.Phase : RaidPhase.InLair,
                _extractionZone != null ? _extractionZone.TimeRemaining : 0f,
                _alarm != null ? _alarm.State : AlarmState.Calm,
                _alarm != null ? _alarm.AlarmLevel : 0f,
                CarriedName(carried),
                carried != null && carried.Data != null && carried.Data.RequiresDualCarry,
                BuildInteractPrompt(carried),
                HasInteractTarget(),
                _lair != null ? _lair.TotalDebt : 0f,
                _lair != null ? _lair.AccumulatedGold : 0f,
                stale ? string.Empty : _lastCastLine,
                _extractionZone != null ? _extractionZone.WorthInZone : 0f,
                _extractionZone != null ? _extractionZone.PiecesInZone : 0,
                _player != null ? _player.CurrentHealth : 0f,
                _player != null ? _player.MaxHealth : 0f,
                BuildEnemyHealthBars());
        }

        /// <summary>
        /// One entry per living guard, positioned above its pivot. Reads <see cref="CastleGuard.Active"/>
        /// rather than a scene search — guards self-register, so this stays cheap with a full castle.
        /// </summary>
        private RaidHudModel.EnemyHealthBar[] BuildEnemyHealthBars()
        {
            var guards = CastleGuard.Active;
            var bars = new RaidHudModel.EnemyHealthBar[guards.Count];
            for (int i = 0; i < guards.Count; i++)
            {
                CastleGuard guard = guards[i];
                float fraction = guard.MaxHealth > 0f ? guard.CurrentHealth / guard.MaxHealth : 0f;
                bars[i] = new RaidHudModel.EnemyHealthBar(
                    guard.transform.position + Vector3.up * _enemyBarHeight, fraction);
            }
            return bars;
        }

        /// <summary>
        /// What the player is holding. Reads <c>ItemManager</c> first — that is the live pickup
        /// system — and falls back to the old interactor while both still exist.
        /// </summary>
        private static string CarriedName(LootPickup legacyCarried)
        {
            Item held = ItemManager.Instance != null ? ItemManager.Instance.CarriedItem : null;
            if (held != null)
            {
                return held.TryGetComponent(out LootValue value) ? value.DisplayName : held.name;
            }

            return legacyCarried != null && legacyCarried.Data != null
                ? legacyCarried.Data.DisplayName
                : string.Empty;
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
                return "Press [E] to open the door";

            LootPickup focus = _interactor.Focus;
            if (focus == null)
                return carried != null ? "Press [Q] to drop" : string.Empty;

            string name = NameOf(focus);

            if (focus.IsBroken)
                return $"{name} — broken, worthless";

            if (focus.Data != null && focus.Data.RequiresDualCarry)
            {
                return focus.IsBeingCarried
                    ? $"Press [E] to take the other end of {name} — needs two"
                    : $"Press [E] to lift {name} — needs two";
            }

            return focus.IsBeingCarried ? string.Empty : $"Press [E] to pick up {name}";
        }

        /// <summary>True while the crosshair is over something the interact key would act on.</summary>
        private bool HasInteractTarget()
        {
            if (_interactor == null)
                return false;
            return _interactor.FocusDoor != null || _interactor.Focus != null;
        }

        private static string NameOf(LootPickup pickup) =>
            pickup != null && pickup.Data != null && !string.IsNullOrEmpty(pickup.Data.DisplayName)
                ? pickup.Data.DisplayName
                : "it";

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
            if (_player == null) _player = FindObjectOfType<PlayerStateMachine>();
        }

        /// <summary>Wires the presenter from code, for tests and tooling-built scenes.</summary>
        public void Configure(RaidDirector director, ExtractionZone zone, AlarmFSMManager alarm,
            LairHubManager lair, LootInteractor interactor, PlayerStateMachine player = null)
        {
            _director = director;
            _extractionZone = zone;
            _alarm = alarm;
            _lair = lair;
            _interactor = interactor;
            _player = player;
        }
    }
}

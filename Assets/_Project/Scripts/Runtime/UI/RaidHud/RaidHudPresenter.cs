using Plunderspell.Voice;
using Plunderspell.Core;
using Code.Scripts.EventSystems;
using Plunderspell.Alarm;
using Plunderspell.Extraction;
using Plunderspell.Lair;
using Plunderspell.Loot;
using Plunderspell.Raid;
using Plunderspell.Spells;
using UnityEngine;

namespace Plunderspell.UI
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
        [SerializeField] private EnemyDirector _alarm;
        [SerializeField] private LairHubManager _lair;
        [SerializeField] private LootInteractor _interactor;

        /// <summary>This machine's player's interactor. The player is spawned by the network after
        /// the HUD wakes, so it is looked up on first use rather than wired in the scene.</summary>
        private LootInteractor Interactor
        {
            get
            {
                if (_interactor == null && StateMachine.PlayerStateMachine.Local != null)
                    _interactor = StateMachine.PlayerStateMachine.Local.GetComponentInChildren<LootInteractor>();
                return _interactor;
            }
        }

        [Header("Cast feed")]
        [Tooltip("Seconds the most recent cast stays on screen.")]
        [SerializeField] private float _castLineDuration = 4f;

        private string _lastCastLine = string.Empty;
        private float _lastCastAt = float.NegativeInfinity;

        /// <summary>The model as of the last change any source published.</summary>
        public RaidHudModel Model { get; private set; }

        /// <summary>The local caster's spell words and costs, for the spell list. Null until that player exists.</summary>
        public SpellLexicon Lexicon { get; private set; }

        // The values behind the model. Each is refreshed only by the event that says it changed, so a frame
        // in which nothing happened builds nothing (#303). Refresh() reads them all from the live sources.
        private RaidPhase _phase = RaidPhase.InLair;
        private float _time;
        private AlarmState _alarmState = AlarmState.Calm;
        private float _alarmLevel;
        private string _carriedName = string.Empty;
        private bool _carriedNeedsTwo;
        private string _prompt = string.Empty;
        private bool _hasTarget;
        private float _debt;
        private float _gold;
        private float _haulWorth;
        private int _haulPieces;
        private string _rangedStatus = string.Empty;
        private bool _casting;
        private string _listenDevice;
        private bool _chanting;
        private string _chantWord = string.Empty;
        private float _chantProgress;
        private int _mana = int.MaxValue;
        private Plunderspell.Core.GameState _state = Plunderspell.Core.GameState.MainMenu;
        private string _castLine = string.Empty;

        private void Awake() => AutoWire();

        private void OnEnable()
        {
            Refresh();

            EventManager bus = EventManager.Instance;
            if (bus == null)
                return;

            bus.Subscribe(this, (RaidPhaseChanged e) => { _phase = e.Phase; Assemble(); });
            bus.Subscribe(this, (ExtractionTimerChanged e) => { _time = e.SecondsRemaining; Assemble(); });
            bus.Subscribe(this, (AlarmChanged e) => { _alarmState = e.State; Assemble(); });
            bus.Subscribe(this, (AlarmLevelChanged e) => { _alarmLevel = e.Level; Assemble(); });
            bus.Subscribe(this, (DebtChanged e) => { _debt = e.Debt; Assemble(); });
            bus.Subscribe(this, (BankedGoldChanged e) => { _gold = e.Gold; Assemble(); });
            bus.Subscribe(this, (HaulInZoneChanged e) => { _haulWorth = e.Worth; _haulPieces = e.Pieces; Assemble(); });
            bus.Subscribe(this, (CarriedItemChanged e) => RefreshInteraction());
            bus.Subscribe(this, (LootFocusChanged e) => RefreshInteraction());
            bus.Subscribe(this, (DoorFocusChanged e) => RefreshInteraction());
            bus.Subscribe(this, (RangedWeaponStatusChanged e) => OnRangedWeaponStatus(e.Weapon));
            bus.Subscribe(this, (CastResolved e) => OnCastResolved(e.Report));
            bus.Subscribe(this, (StateMachine.LocalPlayerChanged e) => OnLocalPlayerChanged(e.Player));
            bus.Subscribe(this, (GameStateChanged e) => { _state = e.Current; RefreshCaster(); Assemble(); });
            bus.Subscribe(this, (PlayerStatsChanged e) => OnStatsChanged());
            bus.Subscribe(this, (CastingStateChanged e) => OnCastingStateChanged(e.Controller, e.Casting));
            bus.Subscribe(this, (ChantProgressChanged e) =>
            {
                _chanting = e.Chanting;
                _chantWord = e.Word;
                _chantProgress = e.Progress;
                Assemble();
            });
        }

        private void OnDisable()
        {
            CancelInvoke();
            EventManager.Instance?.UnsubscribeFromAllEvents(this);
        }

        /// <summary>Reads every source now and rebuilds the model. Public so tests call it directly.</summary>
        public RaidHudModel Build()
        {
            Refresh();
            return Model;
        }

        /// <summary>Reads every source and assembles the model. Run once on enable and by <see cref="Build"/>; events keep it current after.</summary>
        public void Refresh()
        {
            _phase = _director != null ? _director.Phase : RaidPhase.InLair;
            _time = _extractionZone != null ? _extractionZone.TimeRemaining : 0f;
            _alarmState = _alarm != null ? _alarm.State : AlarmState.Calm;
            _alarmLevel = _alarm != null ? _alarm.AlarmLevel : 0f;
            _debt = _lair != null ? _lair.TotalDebt : 0f;
            _gold = _lair != null ? _lair.AccumulatedGold : 0f;
            _haulWorth = _extractionZone != null ? _extractionZone.WorthInZone : 0f;
            _haulPieces = _extractionZone != null ? _extractionZone.PiecesInZone : 0;
            _castLine = Time.time - _lastCastAt > _castLineDuration ? string.Empty : _lastCastLine;
            _state = Plunderspell.Core.GameServices.GameState != null
                ? Plunderspell.Core.GameServices.GameState.CurrentState
                : Plunderspell.Core.GameState.MainMenu;
            _mana = Plunderspell.Core.GameServices.PlayerStats?.Mana ?? int.MaxValue;
            RefreshCaster();
            RefreshInteraction();
        }

        private void RefreshInteraction()
        {
            LootPickup carried = Interactor != null ? Interactor.Carried : null;
            _carriedName = CarriedName(carried);
            _carriedNeedsTwo = carried != null && carried.Data != null && carried.Data.RequiresDualCarry;
            _prompt = BuildInteractPrompt(carried);
            _hasTarget = HasInteractTarget();
            _rangedStatus = BuildRangedWeaponStatus();
            Assemble();
        }

        // The local caster's words, and whether a chant or a held cast key is on. Read when the local player
        // or the game mode changes; chant and cast key then keep themselves current by event.
        private void RefreshCaster()
        {
            SpellCastingSystem caster = SpellCastingSystem.Local;
            Lexicon = caster != null ? caster.Lexicon : null;
            _chanting = caster != null && caster.IsChanting;
            _chantWord = _chanting ? caster.ChantingWord : string.Empty;
            _chantProgress = _chanting ? caster.ChantProgress : 0f;

            PushToCastController push = LocalPushToCast();
            _casting = push != null && push.IsCasting;
            _listenDevice = _casting ? ListeningDevice() : null;
        }

        private static PushToCastController LocalPushToCast() =>
            StateMachine.PlayerStateMachine.Local != null
                ? StateMachine.PlayerStateMachine.Local.GetComponentInChildren<PushToCastController>()
                : null;

        private static string ListeningDevice()
        {
            var speech = (VoiceServiceLocator.Current as CombinedVoiceInputService)?.Speech;
            return speech != null && speech.IsListening ? speech.CurrentDevice : null;
        }

        private void OnCastingStateChanged(PushToCastController controller, bool casting)
        {
            if (controller != LocalPushToCast())
                return;
            _casting = casting;
            _listenDevice = casting ? ListeningDevice() : null;
            Assemble();
        }

        private void OnLocalPlayerChanged(StateMachine.PlayerStateMachine player)
        {
            if (player != null)
                _interactor = player.GetComponentInChildren<LootInteractor>();
            RefreshCaster();
            RefreshInteraction();
        }

        private void OnRangedWeaponStatus(RangedWeapon weapon)
        {
            if (ItemManager.Instance == null || weapon != ItemManager.Instance.CarriedRangedWeapon)
                return;
            _rangedStatus = BuildRangedWeaponStatus();
            Assemble();
        }

        private void OnStatsChanged()
        {
            _mana = Plunderspell.Core.GameServices.PlayerStats?.Mana ?? int.MaxValue;
            if (Lexicon == null)
                Lexicon = SpellCastingSystem.Local != null ? SpellCastingSystem.Local.Lexicon : null; // the caster arrives after the HUD wakes
            Assemble();
        }

        private void Assemble()
        {
            Model = new RaidHudModel(_phase, _time, _alarmState, _alarmLevel, _carriedName, _carriedNeedsTwo,
                _prompt, _hasTarget, _debt, _gold, _castLine, _haulWorth, _haulPieces, _rangedStatus,
                _casting, _listenDevice, _chanting, _chantWord, _chantProgress, _mana, _state);
        }

        /// <summary>What the currently held ranged weapon (if any) is doing right now.</summary>
        private static string BuildRangedWeaponStatus()
        {
            RangedWeapon weapon = ItemManager.Instance != null ? ItemManager.Instance.CarriedRangedWeapon : null;
            if (weapon == null)
                return string.Empty;

            return weapon.IsLoaded
                ? "Loaded — hold [RMB] to aim, [G] to fire"
                : $"Reloading… {Mathf.RoundToInt(weapon.ReloadProgress01 * 100f)}%";
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
            if (Interactor == null)
                return string.Empty;

            if (Interactor.FocusDoor != null)
                return "Press [E] to open the door";

            LootPickup focus = Interactor.Focus;
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
            if (Interactor == null)
                return false;
            return Interactor.FocusDoor != null || Interactor.Focus != null;
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
            _castLine = _lastCastLine;
            // The line fades by timer, not by polling: clear it when its time is up.
            CancelInvoke(nameof(ClearCastLine));
            Invoke(nameof(ClearCastLine), _castLineDuration);
            Assemble();
        }

        private void ClearCastLine()
        {
            _castLine = string.Empty;
            Assemble();
        }

        /// <summary>Finds whatever is in the scene. Called on Awake and by tooling-built scenes.</summary>
        public void AutoWire()
        {
            if (_director == null) _director = FindObjectOfType<RaidDirector>();
            if (_extractionZone == null) _extractionZone = FindObjectOfType<ExtractionZone>();
            if (_alarm == null) _alarm = FindObjectOfType<EnemyDirector>();
            if (_lair == null) _lair = FindObjectOfType<LairHubManager>();
            // No scene search for the interactor: in a session every player's body has one, and only
            // this machine's player (resolved lazily in Interactor) is the one the HUD describes.
        }

        /// <summary>Wires the presenter from code, for tests and tooling-built scenes.</summary>
        public void Configure(RaidDirector director, ExtractionZone zone, EnemyDirector alarm,
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

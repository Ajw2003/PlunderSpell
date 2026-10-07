using Code.Scripts.EventSystems;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Lair
{
    /// <summary>
    /// Owns the persistent between-raids "Lair" meta-progression: era selection, the debt owed to the
    /// mysterious benefactor, and the gold banked from extractions. All state is persisted to
    /// <see cref="PlayerPrefs"/> so it survives across sessions (the Lair is a persistent scene that
    /// hosts between raids). This is a plain <see cref="MonoBehaviour"/> — meta-progression is local
    /// save state, not networked raid state.
    /// </summary>
    public class LairHubManager : MonoBehaviour
    {
        // PlayerPrefs keys.
        private const string KeySelectedEra = "SelectedEra";
        private const string KeyTotalDebt = "TotalDebt";
        private const string KeyAccumulatedGold = "AccumulatedGold";
        private const string KeyPurse = "Purse";

        // Defaults.
        private const float DefaultDebt = 500f;
        private const float DefaultGold = 0f;

        [Header("Config")]
        [SerializeField] private HistoricalEra SelectedEra = HistoricalEra.BronzeAge;
        [Tooltip("Debt added each session while any debt remains (deadline pressure).")]
        public float DebtIncreasePerSession = 50f;

        public float TotalDebt { get; private set; }
        public float AccumulatedGold { get; private set; }

        /// <summary>The wizards a company holds, and so the strongboxes: seat n is strongbox n.</summary>
        public const int Seats = 4;

        private readonly int[] _purses = new int[Seats];

        /// <summary>The coins banked into <paramref name="seat"/>'s strongbox (0 to 3). Saved per slot.</summary>
        public int Purse(int seat) => _purses[seat];

        private void Awake() => Load();

        /// <summary>Load the active save slot's state from PlayerPrefs (falling back to defaults).</summary>
        public void Load()
        {
            LairState state = Peek(SaveSlots.Active);
            SelectedEra = state.SelectedEra;
            TotalDebt = state.TotalDebt;
            AccumulatedGold = state.AccumulatedGold;
            for (int seat = 0; seat < Seats; seat++)
            {
                int old = _purses[seat];
                _purses[seat] = PeekPurse(SaveSlots.Active, seat);
                if (_purses[seat] != old)
                    EventManager.Instance?.Publish(new PurseChanged(seat, _purses[seat]));
            }
            PublishLedger(float.NaN, float.NaN);
            EventManager.Instance?.Publish(new AgeChosen(SelectedEra));
        }

        // Publishes the debt and the banked gold when they differ from the given earlier values (NaN: always).
        private void PublishLedger(float oldDebt, float oldGold)
        {
            if (TotalDebt != oldDebt)
                EventManager.Instance?.Publish(new DebtChanged(TotalDebt));
            if (AccumulatedGold != oldGold)
                EventManager.Instance?.Publish(new BankedGoldChanged(AccumulatedGold));
        }

        /// <summary>Makes <paramref name="slot"/> the save in use and loads it; the last-raid lines belonged to the old one.</summary>
        public void LoadSlot(int slot)
        {
            SaveSlots.Active = slot;
            LastRaidWorth = -1f;
            LastRaidLeftBehind = 0;
            Load();
            EventManager.Instance?.Publish(new SaveSlotLoaded(slot));
        }

        private void Save()
        {
            int slot = SaveSlots.Active;
            PlayerPrefs.SetInt(SaveSlots.Key(KeySelectedEra, slot), (int)SelectedEra);
            PlayerPrefs.SetFloat(SaveSlots.Key(KeyTotalDebt, slot), TotalDebt);
            PlayerPrefs.SetFloat(SaveSlots.Key(KeyAccumulatedGold, slot), AccumulatedGold);
            for (int seat = 0; seat < Seats; seat++)
                PlayerPrefs.SetInt(SaveSlots.Key(KeyPurse + seat, slot), _purses[seat]);
            PlayerPrefs.Save();
        }

        /// <summary>The coins in one seat's purse in <paramref name="slot"/>, without loading it.</summary>
        public static int PeekPurse(int slot, int seat) => PlayerPrefs.GetInt(SaveSlots.Key(KeyPurse + seat, slot), 0);

        /// <summary>A slot's saved state without loading it, defaults where nothing is saved.</summary>
        public static LairState Peek(int slot) => new LairState(
            PlayerPrefs.GetFloat(SaveSlots.Key(KeyTotalDebt, slot), DefaultDebt),
            PlayerPrefs.GetFloat(SaveSlots.Key(KeyAccumulatedGold, slot), DefaultGold),
            (HistoricalEra)PlayerPrefs.GetInt(SaveSlots.Key(KeySelectedEra, slot), (int)HistoricalEra.BronzeAge));

        /// <summary>True once a raid has been banked or an era picked in <paramref name="slot"/>.</summary>
        public static bool HasSave(int slot) =>
            PlayerPrefs.HasKey(SaveSlots.Key(KeyTotalDebt, slot)) || PlayerPrefs.HasKey(SaveSlots.Key(KeySelectedEra, slot));

        /// <summary>Wipes <paramref name="slot"/> back to a new campaign.</summary>
        public static void ResetSlot(int slot)
        {
            PlayerPrefs.DeleteKey(SaveSlots.Key(KeySelectedEra, slot));
            PlayerPrefs.DeleteKey(SaveSlots.Key(KeyTotalDebt, slot));
            PlayerPrefs.DeleteKey(SaveSlots.Key(KeyAccumulatedGold, slot));
            for (int seat = 0; seat < Seats; seat++)
                PlayerPrefs.DeleteKey(SaveSlots.Key(KeyPurse + seat, slot));
            HaulPileSave.Clear(slot);
            PlayerPrefs.Save();
        }

        /// <summary>Select the historical era for the next raid and persist it.</summary>
        public void SelectEra(HistoricalEra era)
        {
            bool changed = era != SelectedEra;
            SelectedEra = era;
            PlayerPrefs.SetInt(SaveSlots.Key(KeySelectedEra, SaveSlots.Active), (int)era);
            PlayerPrefs.Save();
            if (changed)
                EventManager.Instance?.Publish(new AgeChosen(era));
        }

        /// <summary>What the most recent raid brought home, for the Lair's "last raid" line. -1 before any raid.</summary>
        public float LastRaidWorth { get; private set; } = -1f;

        /// <summary>Players left behind when the last raid's portal closed. Not saved: it describes one evening.</summary>
        public int LastRaidLeftBehind { get; private set; }

        /// <summary>Records how many were stuck outside the portal when it closed.</summary>
        public void RecordLeftBehind(int count) => LastRaidLeftBehind = Mathf.Max(0, count);

        /// <summary>
        /// Records what a completed raid carried home, for the "last raid" line. Banks nothing: coins
        /// come only from selling the haul in the Market (<see cref="BankSale"/>).
        /// </summary>
        public void ApplyExtractionResult(float worthExtracted) => LastRaidWorth = Mathf.Max(0f, worthExtracted);

        /// <summary>
        /// Bank the coins from a sale. Gold is banked, then applied toward the debt. When the accumulated
        /// gold covers the full debt the game reaches the endgame stub (debt cleared); otherwise as much
        /// debt as possible is paid down.
        /// </summary>
        public void BankSale(float coins)
        {
            float debtBefore = TotalDebt;
            float goldBefore = AccumulatedGold;
            AccumulatedGold += Mathf.Max(0f, coins);

            if (AccumulatedGold >= TotalDebt)
            {
                // Endgame: the benefactor is fully paid.
                AccumulatedGold -= TotalDebt;
                TotalDebt = 0f;
                Debug.Log("DEBT_CLEARED");
            }
            else
            {
                // Pay down as much of the debt as the banked gold allows.
                float payment = Mathf.Min(AccumulatedGold, TotalDebt);
                AccumulatedGold -= payment;
                TotalDebt = Mathf.Max(0f, TotalDebt - payment);
            }

            PublishLedger(debtBefore, goldBefore);
            Save();
        }

        /// <summary>
        /// A pouch dropped in <paramref name="seat"/>'s strongbox: its coins go into that purse. Until the debt splits in
        /// equal shares (the next step of #306) every purse also pays down the one shared debt, exactly as <see cref="BankSale"/> does.
        /// </summary>
        public void BankPouch(int seat, int coins)
        {
            if (seat < 0 || seat >= Seats || coins <= 0)
                return;
            _purses[seat] += coins;
            EventManager.Instance?.Publish(new PurseChanged(seat, _purses[seat]));
            BankSale(coins); // saves
        }

        /// <summary>
        /// Called at raid start. While any debt remains it ticks up by
        /// <see cref="DebtIncreasePerSession"/> to apply deadline pressure.
        /// </summary>
        public void OnNewSession()
        {
            if (TotalDebt > 0f)
            {
                float debtBefore = TotalDebt;
                TotalDebt += DebtIncreasePerSession;
                PublishLedger(debtBefore, AccumulatedGold);
                Save();
            }
        }

        /// <summary>
        /// Shows the host's campaign on a client in someone else's session: the debt and bank
        /// everyone is paying off together. Deliberately not saved, so joining a friend never
        /// overwrites this machine's own campaign; <see cref="Load"/> puts it back afterwards.
        /// </summary>
        public void ShowHostCampaign(float debt, float gold, float lastRaidWorth)
        {
            float debtBefore = TotalDebt;
            float goldBefore = AccumulatedGold;
            TotalDebt = debt;
            AccumulatedGold = gold;
            LastRaidWorth = lastRaidWorth;
            PublishLedger(debtBefore, goldBefore);
        }

        /// <summary>Snapshot the current lair meta-state.</summary>
        public LairState GetLairState() => new LairState(TotalDebt, AccumulatedGold, SelectedEra);
    }
}

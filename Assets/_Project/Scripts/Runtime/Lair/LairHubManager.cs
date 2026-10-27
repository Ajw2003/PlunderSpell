using Code.Scripts.EventSystems;
using Plunderspell.Inventory;
using Plunderspell.Market;
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
        private const string KeyPaidLast = "PaidLast";

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
        private readonly int[] _paidLast = new int[Seats];
        private readonly bool[] _present = { true, false, false, false };

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
                _paidLast[seat] = PeekPaidLast(SaveSlots.Active, seat);
            }
            CollectorLine = string.Empty;
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
            {
                PlayerPrefs.SetInt(SaveSlots.Key(KeyPurse + seat, slot), _purses[seat]);
                PlayerPrefs.SetInt(SaveSlots.Key(KeyPaidLast + seat, slot), _paidLast[seat]);
            }
            PlayerPrefs.Save();
        }

        /// <summary>What one seat paid at the last collection in <paramref name="slot"/>, without loading it.</summary>
        public static int PeekPaidLast(int slot, int seat) => PlayerPrefs.GetInt(SaveSlots.Key(KeyPaidLast + seat, slot), 0);

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
            {
                PlayerPrefs.DeleteKey(SaveSlots.Key(KeyPurse + seat, slot));
                PlayerPrefs.DeleteKey(SaveSlots.Key(KeyPaidLast + seat, slot));
            }
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

        /// <summary>Shows the Age the host chose on a client (the century dial, #358). Saves nothing, like <see cref="ShowHostLedger"/>.</summary>
        public void ShowHostEra(HistoricalEra era)
        {
            if (era == SelectedEra)
                return;
            SelectedEra = era;
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
        /// A pouch dropped in <paramref name="seat"/>'s strongbox: its coins go into that purse and nothing else. The debt is
        /// paid only when the Collector calls (<see cref="Collect"/>). A friend covers another's share by banking into
        /// that friend's strongbox.
        /// </summary>
        public void BankPouch(int seat, int coins)
        {
            if (seat < 0 || seat >= Seats || coins <= 0)
                return;
            _purses[seat] += coins;
            EventManager.Instance?.Publish(new PurseChanged(seat, _purses[seat]));
            Save();
        }

        /// <summary>How much each seat paid at the last collection. Saved per slot.</summary>
        public int PaidLast(int seat) => _paidLast[seat];

        /// <summary>Whether a wizard sits at <paramref name="seat"/> tonight (set by the host; seat 0 alone when solo).</summary>
        public bool IsPresent(int seat) => _present[seat];

        /// <summary>One wizard's share of the debt left if the Collector called now.</summary>
        public int ShareDue()
        {
            int here = 0;
            foreach (bool seated in _present)
                here += seated ? 1 : 0;
            return CollectorRules.Share(TotalDebt, here);
        }

        /// <summary>The Collector's last line ("The Collector takes 120 from I, 80 from II."), or empty before he has called.</summary>
        public string CollectorLine { get; private set; } = string.Empty;

        /// <summary>Marks which seats have a wizard (a connected player). Published as <see cref="PresentChanged"/> when it differs.</summary>
        public void SetPresent(bool[] present)
        {
            bool changed = false;
            for (int seat = 0; seat < Seats; seat++)
            {
                changed |= _present[seat] != present[seat];
                _present[seat] = present[seat];
            }
            if (changed)
                EventManager.Instance?.Publish(new PresentChanged());
        }

        /// <summary>
        /// The Collector calls, once per setting out, on the server or solo: each wizard present owes an equal share of the debt
        /// left, and he takes the lesser of that share and their purse (<see cref="CollectorRules"/>). The debt is paid by the sum.
        /// </summary>
        public void Collect()
        {
            if (TotalDebt <= 0f)
                return;
            int[] taken = CollectorRules.Take(TotalDebt, _purses, _present);
            float debtBefore = TotalDebt;
            var parts = new System.Collections.Generic.List<string>();
            int sum = 0;
            for (int seat = 0; seat < Seats; seat++)
            {
                _paidLast[seat] = taken[seat];
                _purses[seat] -= taken[seat];
                sum += taken[seat];
                EventManager.Instance?.Publish(new PurseChanged(seat, _purses[seat]));
                EventManager.Instance?.Publish(new CollectorPaid(seat, taken[seat]));
                if (taken[seat] > 0)
                    parts.Add($"{taken[seat]} from {Numerals[seat]}");
            }
            TotalDebt = Mathf.Max(0f, TotalDebt - sum);
            if (TotalDebt <= 0f)
                Debug.Log("DEBT_CLEARED");
            CollectorLine = parts.Count > 0 ? $"The Collector takes {string.Join(", ", parts)}." : "The Collector finds the purses empty.";
            Debug.Log($"[Lair] {CollectorLine} Debt {debtBefore} -> {TotalDebt}.");
            EventManager.Instance?.Publish(new CollectorSpoke(CollectorLine));
            PublishLedger(debtBefore, AccumulatedGold);
            Save();
        }

        private static readonly string[] Numerals = { "I", "II", "III", "IV" };

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

        /// <summary>
        /// The per-seat ledger as one string for the host to replicate: purses, paid last, seats present, then the
        /// Collector's line (last, so it may hold anything but the separator).
        /// </summary>
        public string HostLedger()
        {
            string purses = string.Join(",", _purses);
            string paid = string.Join(",", _paidLast);
            string present = string.Join(",", System.Array.ConvertAll(_present, seated => seated ? 1 : 0));
            return $"{purses}|{paid}|{present}|{CollectorLine}";
        }

        /// <summary>
        /// Shows the host's purses, paid-last, seats present and Collector line on a client (see <see cref="HostLedger"/>).
        /// Raises the events the ledger listens to and, like <see cref="ShowHostCampaign"/>, saves nothing.
        /// </summary>
        public void ShowHostLedger(string ledger)
        {
            string[] parts = ledger.Split(new[] { '|' }, 4);
            if (parts.Length < 4)
                return;
            string[] purses = parts[0].Split(',');
            string[] paid = parts[1].Split(',');
            string[] present = parts[2].Split(',');
            if (purses.Length != Seats || paid.Length != Seats || present.Length != Seats)
                return;

            var seated = new bool[Seats];
            for (int seat = 0; seat < Seats; seat++)
            {
                int coins = int.Parse(purses[seat]);
                int taken = int.Parse(paid[seat]);
                seated[seat] = present[seat] == "1";
                bool purseChanged = coins != _purses[seat];
                bool paidChanged = taken != _paidLast[seat];
                _purses[seat] = coins;
                _paidLast[seat] = taken;
                if (purseChanged)
                    EventManager.Instance?.Publish(new PurseChanged(seat, coins));
                if (paidChanged)
                    EventManager.Instance?.Publish(new CollectorPaid(seat, taken));
            }
            SetPresent(seated);
            if (parts[3] != CollectorLine)
            {
                CollectorLine = parts[3];
                if (CollectorLine.Length > 0)
                    EventManager.Instance?.Publish(new CollectorSpoke(CollectorLine));
            }
        }

        /// <summary>Snapshot the current lair meta-state.</summary>
        public LairState GetLairState() => new LairState(TotalDebt, AccumulatedGold, SelectedEra);
    }
}

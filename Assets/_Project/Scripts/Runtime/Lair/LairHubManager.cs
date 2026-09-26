using RogueAi.Inventory;
using UnityEngine;

namespace RogueAi.Lair
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

        // Defaults.
        private const float DefaultDebt = 500f;
        private const float DefaultGold = 0f;

        [Header("Config")]
        [SerializeField] private HistoricalEra SelectedEra = HistoricalEra.BronzeAge;
        [Tooltip("Debt added each session while any debt remains (deadline pressure).")]
        public float DebtIncreasePerSession = 50f;

        public float TotalDebt { get; private set; }
        public float AccumulatedGold { get; private set; }

        private void Awake() => Load();

        /// <summary>Load persisted state from PlayerPrefs (falling back to defaults).</summary>
        public void Load()
        {
            SelectedEra = (HistoricalEra)PlayerPrefs.GetInt(KeySelectedEra, (int)HistoricalEra.BronzeAge);
            TotalDebt = PlayerPrefs.GetFloat(KeyTotalDebt, DefaultDebt);
            AccumulatedGold = PlayerPrefs.GetFloat(KeyAccumulatedGold, DefaultGold);
        }

        private void Save()
        {
            PlayerPrefs.SetInt(KeySelectedEra, (int)SelectedEra);
            PlayerPrefs.SetFloat(KeyTotalDebt, TotalDebt);
            PlayerPrefs.SetFloat(KeyAccumulatedGold, AccumulatedGold);
            PlayerPrefs.Save();
        }

        /// <summary>Select the historical era for the next raid and persist it.</summary>
        public void SelectEra(HistoricalEra era)
        {
            SelectedEra = era;
            PlayerPrefs.SetInt(KeySelectedEra, (int)era);
            PlayerPrefs.Save();
        }

        /// <summary>What the most recent raid brought home, for the Lair's "last raid" line. -1 before any raid.</summary>
        public float LastRaidWorth { get; private set; } = -1f;

        /// <summary>Players left behind when the last raid's portal closed. Not saved: it describes one evening.</summary>
        public int LastRaidLeftBehind { get; private set; }

        /// <summary>Records how many were stuck outside the portal when it closed.</summary>
        public void RecordLeftBehind(int count) => LastRaidLeftBehind = Mathf.Max(0, count);

        /// <summary>
        /// Apply the worth extracted from a completed raid. Gold is banked, then applied toward the
        /// debt. When the accumulated gold covers the full debt the game reaches the endgame stub
        /// (debt cleared); otherwise as much debt as possible is paid down.
        /// </summary>
        public void ApplyExtractionResult(float worthExtracted)
        {
            LastRaidWorth = Mathf.Max(0f, worthExtracted);
            AccumulatedGold += Mathf.Max(0f, worthExtracted);

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

            Save();
        }

        /// <summary>
        /// Called at raid start. While any debt remains it ticks up by
        /// <see cref="DebtIncreasePerSession"/> to apply deadline pressure.
        /// </summary>
        public void OnNewSession()
        {
            if (TotalDebt > 0f)
            {
                TotalDebt += DebtIncreasePerSession;
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
            TotalDebt = debt;
            AccumulatedGold = gold;
            LastRaidWorth = lastRaidWorth;
        }

        /// <summary>Snapshot the current lair meta-state.</summary>
        public LairState GetLairState() => new LairState(TotalDebt, AccumulatedGold, SelectedEra);
    }
}

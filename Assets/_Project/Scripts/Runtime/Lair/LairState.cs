using System;
using Plunderspell.Inventory;

namespace Plunderspell.Lair
{
    /// <summary>
    /// Serializable snapshot of the between-raids Lair meta-state: how much debt remains, how much
    /// gold has been banked, and which historical era the player has selected for the next raid.
    /// </summary>
    [Serializable]
    public struct LairState
    {
        public float TotalDebt;
        public float AccumulatedGold;
        public HistoricalEra SelectedEra;

        public LairState(float totalDebt, float accumulatedGold, HistoricalEra selectedEra)
        {
            TotalDebt = totalDebt;
            AccumulatedGold = accumulatedGold;
            SelectedEra = selectedEra;
        }
    }
}

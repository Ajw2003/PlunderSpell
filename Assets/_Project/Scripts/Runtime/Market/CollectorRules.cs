using System;

namespace Plunderspell.Market
{
    /// <summary>
    /// The Collector's arithmetic (#313, decision 2026-10-07 "equal shares"): when the company sets out, each wizard
    /// present owes an equal share of the debt left, and he takes the lesser of the purse and the share. No Unity here,
    /// so the rules are tested by plain .NET (Tools/MarketRules).
    /// </summary>
    public static class CollectorRules
    {
        /// <summary>One wizard's share: the debt split among those present, rounded up to a whole coin. 0 when nobody is present.</summary>
        public static int Share(float debt, int present) =>
            present <= 0 || debt <= 0f ? 0 : (int)Math.Ceiling(debt / present);

        /// <summary>
        /// What the Collector takes from each seat: min(purse, share) for seats marked present, 0 for the rest. The
        /// total never exceeds the debt (rounding up could otherwise take a coin too many from the last seats).
        /// </summary>
        public static int[] Take(float debt, int[] purses, bool[] present)
        {
            int here = 0;
            for (int seat = 0; seat < present.Length; seat++)
                if (present[seat])
                    here++;

            int share = Share(debt, here);
            int owed = (int)Math.Ceiling(Math.Max(0f, debt));
            var taken = new int[purses.Length];
            for (int seat = 0; seat < purses.Length; seat++)
            {
                if (!present[seat])
                    continue;
                taken[seat] = Math.Min(Math.Min(purses[seat], share), owed);
                owed -= taken[seat];
            }
            return taken;
        }
    }
}

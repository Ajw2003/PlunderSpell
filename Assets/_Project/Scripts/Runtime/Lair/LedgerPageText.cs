using System.Globalization;

namespace Plunderspell.Lair
{
    /// <summary>
    /// The words on the two pages of the ledger book on the Lair table (#357): as plain functions of the
    /// ledger's values (the Lair screen that once showed them is gone, #359). Numbers use the invariant culture so both players' books read alike.
    /// </summary>
    public static class LedgerPageText
    {
        private static readonly string[] Numerals = { "I", "II", "III", "IV" };

        private static string Coins(float value) => value.ToString("N0", CultureInfo.InvariantCulture);

        /// <summary>Left page: what is owed, how fast it grows, and what the last raid brought home.</summary>
        public static string LeftPage(float debt, float debtPerRaid, float lastRaidWorth, int leftBehind)
        {
            string behind = leftBehind == 0 ? string.Empty : $" · {leftBehind} left behind";
            string lastRaid = lastRaidWorth < 0f ? "No raid yet."
                : lastRaidWorth > 0f ? $"Brought home {Coins(lastRaidWorth)} coin{behind}"
                : $"Came home with nothing{behind}";
            return $"<size=130%>Owed</size>\n<size=220%>{Coins(debt)}</size>\nthe debt grows by {Coins(debtPerRaid)} each raid it stands\n\n<size=130%>Last raid</size>\n{lastRaid}";
        }

        /// <summary>Right page: one block per seat that has a purse, owes tonight or paid last time, then the Collector's line.</summary>
        public static string RightPage(int[] purses, int[] paidLast, bool[] present, int shareDue, string collectorLine)
        {
            var page = new System.Text.StringBuilder("<size=130%>Purses</size>\n");
            for (int seat = 0; seat < Numerals.Length; seat++)
            {
                if (!present[seat] && purses[seat] <= 0 && paidLast[seat] <= 0)
                    continue;
                page.Append($"{Numerals[seat]}  Purse {Coins(purses[seat])}\n    Owes {Coins(present[seat] ? shareDue : 0)}  Paid {Coins(paidLast[seat])}\n");
            }
            if (collectorLine.Length > 0)
                page.Append('\n').Append(collectorLine);
            return page.ToString();
        }
    }
}

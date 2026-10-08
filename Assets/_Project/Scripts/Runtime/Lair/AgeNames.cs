using Plunderspell.Inventory;

namespace Plunderspell.Lair
{
    /// <summary>
    /// The four Ages in the order the century dial turns through them (#358), with each one's name, stratum, date
    /// and one-line character from the pitch bible. The Lair screen's cards and the dial's plaque read the same words.
    /// </summary>
    public static class AgeNames
    {
        public static readonly HistoricalEra[] Order =
        {
            HistoricalEra.BronzeAge,
            HistoricalEra.HighMedieval,
            HistoricalEra.LateMedieval,
            HistoricalEra.AgeOfPowder,
        };

        public static readonly string[] Strata = { "Stratum I", "Stratum II", "Stratum III", "Stratum IV" };
        public static readonly string[] Dates = { "c. 1200 BC", "c. 1250", "c. 1450", "c. 1620" };
        public static readonly string[] Blurbs =
        {
            "Painted plaster, grain stores, and kings who are also gods. Fire runs faster here.",
            "Curtain walls, spiral stairs, and a chapel worth more than everything around it.",
            "Fortresses within fortresses, built by men who had you in mind. They hunt in pairs.",
            "Glass by the acre and magazines of black powder. One stray Ignis ends the evening.",
        };

        /// <summary>Where <paramref name="era"/> sits in <see cref="Order"/> (0 for an Age the dial does not know).</summary>
        public static int IndexOf(HistoricalEra era) => System.Math.Max(0, System.Array.IndexOf(Order, era));

        /// <summary>The Age after <paramref name="era"/>, going round from the last to the first.</summary>
        public static HistoricalEra Next(HistoricalEra era) => Order[(IndexOf(era) + 1) % Order.Length];

        public static string Label(HistoricalEra era)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge: return "Bronze Age";
                case HistoricalEra.HighMedieval: return "High Medieval";
                case HistoricalEra.LateMedieval: return "Late Medieval";
                case HistoricalEra.AgeOfPowder: return "Age of Powder";
                default: return era.ToString();
            }
        }

        /// <summary>The plaque's words: name large, then stratum and date, then the blurb.</summary>
        public static string Plaque(HistoricalEra era)
        {
            int i = IndexOf(era);
            return $"<size=170%>{Label(era)}</size>\n{Strata[i]} · {Dates[i]}\n\n{Blurbs[i]}";
        }
    }
}

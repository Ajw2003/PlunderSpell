using Plunderspell.Market;

namespace Plunderspell.Raid
{
    /// <summary>The words on a Market counter's chalk slate, worked out from the vendor, whether a haggle is open and his line.</summary>
    public static class SlateText
    {
        public const string Keys = "1 Plus · 2 Satis · 3 Vale";

        /// <summary>What he buys, as HaggleRules.Interest rates it.</summary>
        public static string Wants(Vendor vendor)
        {
            switch (vendor)
            {
                case Vendor.Goldsmith: return "buys metal dearly";
                case Vendor.Pardoner: return "buys holy things dearly";
                case Vendor.Antiquarian: return "buys curios and arms dearly";
                default: return "buys anything";
            }
        }

        /// <summary>
        /// <paramref name="line"/> is a <see cref="VendorLines"/> line ("The Goldsmith: Very well. 138 coin.") or empty.
        /// No line: his name and what he wants. A line: his name over his words; while the haggle is open, the keys below, smaller.
        /// </summary>
        public static string For(Vendor vendor, bool haggleOpen, string line)
        {
            string name = VendorLines.Name(vendor);
            if (string.IsNullOrEmpty(line))
                return name + " - " + Wants(vendor);
            string reply = line.StartsWith(name + ": ") ? line.Substring(name.Length + 2) : line;
            string text = name + "\n" + reply;
            return haggleOpen ? text + "\n<size=60%>" + Keys + "</size>" : text;
        }
    }
}

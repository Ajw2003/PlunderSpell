using Plunderspell.Market;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>What each vendor looks like and says, as subtitle lines. Placeholder wording until the vendors are voiced.</summary>
    public static class VendorLines
    {
        public static string Name(Vendor vendor) => "The " + vendor;

        public static Color Colour(Vendor vendor)
        {
            switch (vendor)
            {
                case Vendor.Goldsmith: return new Color(0.85f, 0.65f, 0.15f);
                case Vendor.Pardoner: return new Color(0.55f, 0.2f, 0.6f);
                case Vendor.Antiquarian: return new Color(0.2f, 0.5f, 0.55f);
                default: return new Color(0.45f, 0.3f, 0.2f);
            }
        }

        public static string Opening(Vendor vendor, int coins) => Say(vendor, $"{coins} coin.");
        public static string Raised(Vendor vendor, int coins) => Say(vendor, $"Very well. {coins} coin.");
        public static string Refused(Vendor vendor) => Say(vendor, vendor == Vendor.Fence ? "No." : "Too much. Do not push me.");
        public static string WillNotBuy(Vendor vendor) => Say(vendor, "Enough. I will not take that tonight.");
        public static string Sold(Vendor vendor, int coins) => Say(vendor, $"{coins} coin, counted out. Done.");
        public static string Farewell(Vendor vendor) => Say(vendor, "Farewell.");
        public static string Worthless(Vendor vendor) => Say(vendor, "There is nothing here worth a coin.");

        private static string Say(Vendor vendor, string line) => Name(vendor) + ": " + line;
    }
}

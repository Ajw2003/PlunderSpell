using System;

namespace Plunderspell.Market
{
    public enum Vendor
    {
        Fence,
        Goldsmith,
        Pardoner,
        Antiquarian,
    }

    /// <summary>
    /// The numbers behind a haggle, from docs/plans/diegetic-ui-lair-market.md ("The numbers behind it").
    /// Every coefficient is a starting point to tune.
    /// </summary>
    public static class HaggleRules
    {
        /// <summary>Each Plus asks this much times the coins on the counter.</summary>
        public const float PlusStep = 1.1f;

        public const float MoodLow = 0.85f, MoodHigh = 1.15f;
        public const float OpeningLow = 0.55f, OpeningHigh = 0.7f;

        /// <summary>Coming back the same night after Vale: the opening offer is this much of what it would be.</summary>
        public const float CameBackOpening = 0.9f;

        /// <summary>How many refusals each vendor takes before he will not buy the piece tonight.</summary>
        public static int Patience(Vendor vendor)
        {
            switch (vendor)
            {
                case Vendor.Fence: return 1;
                case Vendor.Goldsmith: return 3;
                case Vendor.Pardoner: return 3;
                case Vendor.Antiquarian: return 4;
                default: throw new ArgumentOutOfRangeException(nameof(vendor), vendor, null);
            }
        }

        /// <summary>Tonight's mood for a vendor, 0.85-1.15. Rolled once per vendor per night.</summary>
        public static float RollMood(Random night) => Between(night, MoodLow, MoodHigh);

        /// <summary>
        /// Opens a haggle. <paramref name="worth"/> is the piece's value times its condition;
        /// <paramref name="interest"/> (0.6-1.5) is how much this vendor wants it.
        /// </summary>
        public static Haggle Open(Vendor vendor, float worth, float interest, float mood, Random rng, bool cameBack)
        {
            float limit = worth * interest * mood;
            float opening = limit * Between(rng, OpeningLow, OpeningHigh) * (cameBack ? CameBackOpening : 1f);
            return new Haggle(limit, opening, Patience(vendor));
        }

        private static float Between(Random rng, float low, float high) => low + (float)rng.NextDouble() * (high - low);
    }
}

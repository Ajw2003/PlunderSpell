using System;

namespace Plunderspell.Market
{
    /// <summary>The three haggling words (docs/plans/diegetic-ui-lair-market.md, "One haggle").</summary>
    public enum HaggleWord
    {
        /// <summary>"More": ask 10% above the coins on the counter.</summary>
        Plus,
        /// <summary>"Enough": take the coins on the counter.</summary>
        Satis,
        /// <summary>"Farewell": pick the piece up and leave.</summary>
        Vale,
    }

    public enum HaggleOutcome
    {
        /// <summary>The vendor met the ask; the offer went up.</summary>
        Raised,
        /// <summary>The ask was above his limit; his patience dropped and the offer stands.</summary>
        Refused,
        /// <summary>His patience ran out: he will not buy this piece tonight.</summary>
        WillNotBuy,
        /// <summary>Satis: sold for the coins on the counter.</summary>
        Sold,
        /// <summary>Vale: the player left with the piece.</summary>
        WalkedAway,
    }

    /// <summary>
    /// One haggle over one piece with one vendor. Pure: no Unity, no randomness of its own (the opening
    /// numbers come in from <see cref="HaggleRules.Open"/>), so every rule is testable.
    /// </summary>
    public sealed class Haggle
    {
        /// <summary>The most the vendor will pay. Hidden from the player.</summary>
        public float Limit { get; }

        /// <summary>The coins on the counter now.</summary>
        public float Offer { get; private set; }

        /// <summary>How many more refusals the vendor will take. At zero he will not buy the piece tonight.</summary>
        public int Patience { get; private set; }

        public bool IsOver { get; private set; }

        public Haggle(float limit, float openingOffer, int patience)
        {
            if (limit <= 0f || openingOffer <= 0f || patience <= 0)
                throw new ArgumentOutOfRangeException(nameof(limit), "A haggle needs a positive limit, offer and patience.");
            Limit = limit;
            Offer = Math.Min(openingOffer, limit);
            Patience = patience;
        }

        public HaggleOutcome Answer(HaggleWord word)
        {
            if (IsOver)
                throw new InvalidOperationException("This haggle is over.");

            switch (word)
            {
                case HaggleWord.Satis:
                    IsOver = true;
                    return HaggleOutcome.Sold;
                case HaggleWord.Vale:
                    IsOver = true;
                    return HaggleOutcome.WalkedAway;
                default:
                    return AskForMore();
            }
        }

        private HaggleOutcome AskForMore()
        {
            float ask = Offer * HaggleRules.PlusStep;
            if (ask <= Limit)
            {
                Offer = ask;
                return HaggleOutcome.Raised;
            }

            // The design's "2 patience above 1.2 x the limit" cannot happen: the offer never exceeds the limit
            // and a Plus asks 1.1 x the offer. Left out until asks can be larger (docs/plans/lair-market-in-engine.md).
            Patience--;
            if (Patience > 0)
                return HaggleOutcome.Refused;

            Patience = 0;
            IsOver = true;
            return HaggleOutcome.WillNotBuy;
        }
    }
}

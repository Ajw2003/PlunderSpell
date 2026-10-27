using PurrNet;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// A sale's coins as a thing on the counter: carried like loot (it has a LootPickup and an Item) and heavier with
    /// every coin. It is deliberately NOT loot: it has no LootValue, which is all that extraction, the haul pile and
    /// the Market's counters look for. Dropped in a <see cref="LairStrongbox"/> it banks. See docs/4-systems/market.md.
    /// </summary>
    public class CoinPouch : NetworkBehaviour
    {
        public const float KilosPerCoin = 0.01f;
        public const float MinKilos = 0.3f;

        private readonly SyncVar<int> _coins = new SyncVar<int>(0);

        public int Coins => _coins.value;

        /// <summary>What a pouch of <paramref name="coins"/> weighs.</summary>
        public static float WeightFor(int coins) => Mathf.Max(MinKilos, coins * KilosPerCoin);

        private bool _emptied;

        /// <summary>Hands the coins over, once: a pouch still touching the box next physics step gives nothing more.</summary>
        public int TakeCoins()
        {
            int coins = _emptied ? 0 : Coins;
            _emptied = true;
            return coins;
        }

        private void Awake() => _coins.onChanged += _ => Weigh();

        /// <summary>Puts the coins in, on the machine that decides (the server, or an unspawned pouch).</summary>
        public void Fill(int coins)
        {
            _coins.value = coins;
            Weigh();
        }

        private void Weigh()
        {
            if (TryGetComponent(out Rigidbody body))
                body.mass = WeightFor(Coins);
        }
    }
}

using Plunderspell.Lair;
using Plunderspell.Loot;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>
    /// One wizard's strongbox, a trigger on its lid (strongbox n is seat n, the player whose owner id is n, as the
    /// spawns). A <see cref="CoinPouch"/> let go inside it is banked into that seat's purse and removed. Only the
    /// machine that decides acts: the server, or any machine when the pouch is not networked.
    /// </summary>
    public class LairStrongbox : MonoBehaviour
    {
        [Tooltip("Seat 0 to 3: strongbox 1 is seat 0.")]
        [SerializeField] private int _seat;

        public int Seat => _seat;

        public void SetSeat(int seat) => _seat = seat;

        private void OnTriggerStay(Collider other)
        {
            CoinPouch pouch = other.GetComponentInParent<CoinPouch>();
            if (pouch != null)
                Accept(pouch);
        }

        /// <summary>Banks a let-go pouch into this seat's purse and removes it. Does nothing in a hand or on a client.</summary>
        public void Accept(CoinPouch pouch)
        {
            if (pouch.isSpawned && !pouch.isServer)
                return;
            if (pouch.TryGetComponent(out LootPickup pickup) && pickup.IsBeingCarried)
                return; // still in a hand
            FindFirstObjectByType<LairHubManager>()?.BankPouch(_seat, pouch.TakeCoins());
            Destroy(pouch.gameObject); // on a spawned pouch the server's destroy despawns it for every client
        }
    }
}

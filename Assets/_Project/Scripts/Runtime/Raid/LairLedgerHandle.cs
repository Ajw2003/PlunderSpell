using Interfaces;
using Plunderspell.Core;
using UnityEngine;

namespace Plunderspell.Raid
{
    /// <summary>The ledger table: E opens today's Lair screen (debt, banked gold, Age, company).</summary>
    public class LairLedgerHandle : MonoBehaviour, IInteractable
    {
        public void Interact()
        {
            if (GameServices.GameState.CurrentState == GameState.LairRoom)
                GameServices.GameState.ChangeState(GameState.Lair);
        }
    }
}

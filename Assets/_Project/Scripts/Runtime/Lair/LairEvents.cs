using EventSystems;
using Plunderspell.Inventory;

namespace Plunderspell.Lair
{
    // The Lair's ledger and choices, published by LairHubManager when they change (#302).

    /// <summary>What the player still owes changed.</summary>
    public readonly struct DebtChanged : IEvent
    {
        public readonly float Debt;
        public DebtChanged(float debt) { Debt = debt; }
    }

    /// <summary>The gold banked against the debt changed.</summary>
    public readonly struct BankedGoldChanged : IEvent
    {
        public readonly float Gold;
        public BankedGoldChanged(float gold) { Gold = gold; }
    }

    /// <summary>A different save slot was made active and loaded (the Lair's floor pile follows it).</summary>
    public readonly struct SaveSlotLoaded : IEvent
    {
        public readonly int Slot;
        public SaveSlotLoaded(int slot) { Slot = slot; }
    }

    /// <summary>The Age (historical era) for the next raid was chosen or loaded.</summary>
    public readonly struct AgeChosen : IEvent
    {
        public readonly HistoricalEra Era;
        public AgeChosen(HistoricalEra era) { Era = era; }
    }
}

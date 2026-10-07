namespace Interfaces
{
    /// <summary>Anything the interact key (E) can use that is not loot or a door, e.g. the Lair ledger.</summary>
    public interface IInteractable
    {
        void Interact();
    }
}

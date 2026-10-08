using EventSystems;

/// <summary>The item the player drags or carries changed; <see cref="Item"/> is null when they let go (#302).</summary>
public readonly struct CarriedItemChanged : IEvent
{
    public readonly Item Item;
    public CarriedItemChanged(Item item) { Item = item; }
}

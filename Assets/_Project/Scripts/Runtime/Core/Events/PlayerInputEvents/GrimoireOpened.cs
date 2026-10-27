using EventSystems;

/// <summary>
/// The grimoire was opened (<see cref="Open"/> true, the local player holds <c>Tab</c>) or closed (#325). Reading it takes
/// both hands: the player walks slower and cannot pick anything up while it is open. Published on this machine only.
/// </summary>
public readonly struct GrimoireOpened : IEvent
{
    public readonly bool Open;
    public GrimoireOpened(bool open) { Open = open; }
}

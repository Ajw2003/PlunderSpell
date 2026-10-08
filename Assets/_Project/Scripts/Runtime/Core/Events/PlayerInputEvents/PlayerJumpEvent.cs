using EventSystems;

// Published when the jump input is performed (enable true) and when it is cancelled (enable false).
public struct PlayerJumpEvent : IEvent
{
    public bool enable;
}

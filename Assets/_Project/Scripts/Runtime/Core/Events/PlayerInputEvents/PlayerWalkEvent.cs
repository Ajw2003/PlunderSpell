using EventSystems;

// Published when the move input starts or changes (enable true) and when it is cancelled (enable false).
public struct PlayerWalkEvent : IEvent
{
    public bool enable;
}

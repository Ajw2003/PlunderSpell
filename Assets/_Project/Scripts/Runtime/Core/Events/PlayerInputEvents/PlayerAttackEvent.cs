using EventSystems;

// Published when the attack input is performed (enable true) and when it is cancelled (enable false).
public struct PlayerAttackEvent : IEvent
{
    public bool enable;
}

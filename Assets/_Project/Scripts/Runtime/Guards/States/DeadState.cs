using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Down for good (#213). On entry the guard leaves every shared system: its mover is unregistered from
    /// the navigation service (no more steps or separation pushes), it leaves the director's registry (which
    /// also takes back an attack turn it held), and its eyes and ears close. Nothing leaves this state.
    ///
    /// The topple, the fade to dust and the despawn are not here: every peer must see them, and a client
    /// never runs states, so <see cref="GuardDeathPlayback"/> plays them from the replicated Dead state.
    /// </summary>
    public sealed class DeadState : GuardState
    {
        public DeadState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Dead;

        public override void Enter()
        {
            Context.Navigator.Stop();
            Context.Navigator.Detach();
            Context.Link.Detach();
            Context.Sight.Close();
            Context.Hearing.Close();
        }

        public override State<Guard> Tick(float deltaTime) => this;
    }
}

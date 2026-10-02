using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// PLACEHOLDER for Stunned / Slept (#211). The guard stands still while its status effect lasts and
    /// goes back to patrol when it ends. The real state picks Patrol or Investigate by the alarm level
    /// and handles Levo's recover-on-landing (owner's answer 2). Replace this class, do not extend it.
    /// </summary>
    public sealed class PlaceholderIncapacitatedState : GuardState
    {
        public PlaceholderIncapacitatedState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Incapacitated;

        public override void Enter() => Context.Navigator.Stop();

        public override State<Guard> Tick(float deltaTime)
        {
            return Context.Status.IsIncapacitated ? this : Context.States.Patrol;
        }
    }
}

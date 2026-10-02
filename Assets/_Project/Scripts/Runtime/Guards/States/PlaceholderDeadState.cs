using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// PLACEHOLDER for Dead (#213). It stops the guard and removes it at once, as the legacy guard did.
    /// The real state topples the body, fades it to dust and despawns it over the network.
    /// Replace this class, do not extend it.
    /// </summary>
    public sealed class PlaceholderDeadState : GuardState
    {
        public PlaceholderDeadState(Guard guard) : base(guard)
        {
        }

        public override GuardAlertState AlertState => GuardAlertState.Dead;

        public override void Enter()
        {
            Context.Navigator.Stop();
            Object.Destroy(Context.gameObject);
        }
    }
}

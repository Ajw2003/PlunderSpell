namespace Plunderspell.Guards
{
    /// <summary>
    /// How a guard answers another guard's call for help against a player it cannot reach (#237). Only a
    /// guard that is free (on patrol or looking into something) answers; one already fighting stays on its
    /// job. A ranged guard goes straight after the player at chase speed, so it arrives first and shoots.
    /// A melee guard walks to where the player is at the slower investigate pace, finds the player is out of
    /// reach, and holds below like the caller. Server only.
    /// </summary>
    public sealed class GuardHelpResponse
    {
        private readonly Guard _guard;

        public GuardHelpResponse(Guard guard)
        {
            _guard = guard;
            guard.Link.HelpCalled += Answer;
        }

        private void Answer(UnityEngine.Transform player)
        {
            if (!_guard.IsAuthority || _guard.IsDead)
                return;

            GuardStateSet states = _guard.States;
            if (_guard.CurrentState != states.Patrol && _guard.CurrentState != states.Investigate)
                return;

            if (_guard.RangedAttack.IsRanged)
            {
                states.Chase.Follow(player);
                _guard.ChangeState(states.Chase);
            }
            else
            {
                _guard.Leads.Offer(player.position, GuardLeads.SightingStrength);
            }
        }
    }
}

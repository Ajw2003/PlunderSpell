using StateMachine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// Where a guard goes once nothing holds it any more (a stun, a sleep or a fire has ended). Kept in one
    /// place so every state that lets go of the guard applies the same rule.
    /// </summary>
    public static class GuardRecovery
    {
        /// <summary>
        /// A waiting lead (a noise heard while down) means go and look. With no lead the guard still goes to
        /// look around where it stands once the castle is Roused or worse, and otherwise returns to its round.
        /// </summary>
        public static State<Guard> PatrolOrInvestigate(Guard guard)
        {
            bool onAlert = guard.Link.Alarm >= guard.Tuning.InvestigateAfterRecoveryFrom;
            if (!guard.Leads.HasLead && onAlert)
                guard.Leads.Offer(guard.transform.position, 0f);

            return guard.Leads.HasLead ? guard.States.Investigate : guard.States.Patrol;
        }
    }
}

namespace RogueAi.Guards
{
    /// <summary>
    /// What one guard is doing. Ordered by how much trouble the players are in, so escalation can be
    /// compared numerically.
    /// </summary>
    public enum GuardAlertState
    {
        /// <summary>Walking the patrol route, noticing nothing.</summary>
        Patrolling = 0,

        /// <summary>Heard something. Walking to where the noise came from, still not hostile.</summary>
        Investigating = 1,

        /// <summary>Has eyes on an intruder and is closing.</summary>
        Chasing = 2,

        /// <summary>Lost sight of the intruder. Sweeping around where they were last seen.</summary>
        Searching = 3,

        /// <summary>Asleep, stunned or otherwise out of the fight.</summary>
        Incapacitated = 4
    }
}

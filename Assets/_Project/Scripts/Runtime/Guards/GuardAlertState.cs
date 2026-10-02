namespace Plunderspell.Guards
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
        Incapacitated = 4,
        /// <summary>Fighting a player in reach: takes its turn to attack (the Combat state, #210).</summary>
        Combat = 5,
        /// <summary>Burning and running about at random (the OnFire state, #212).</summary>
        OnFire = 6,
        /// <summary>Down for good, toppling and fading (the Dead state, #213).</summary>
        Dead = 7
    }
}

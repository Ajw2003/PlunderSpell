namespace Plunderspell.Raid
{
    /// <summary>
    /// Where a session is in the loop. Ordered as it is lived: the Lair, then a raid, then back to
    /// the Lair with (or without) the takings.
    /// </summary>
    public enum RaidPhase
    {
        /// <summary>Between raids: spend, equip, choose an era. No castle exists.</summary>
        InLair = 0,

        /// <summary>The castle is being built from the seed. Brief, and identical on every peer.</summary>
        Generating = 1,

        /// <summary>The raid proper: the timer runs, the alarm can rise, loot can be taken.</summary>
        Raiding = 2,

        /// <summary>The timer has run out or extraction was called. The zone is tallying.</summary>
        Extracting = 3,

        /// <summary>Resolved: worth banked, debt paid down. The summary is on screen.</summary>
        Resolved = 4
    }
}

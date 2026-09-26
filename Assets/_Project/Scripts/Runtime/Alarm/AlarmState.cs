namespace Plunderspell.Alarm
{
    /// <summary>
    /// Castle-wide alert level. Ordered by severity so escalation/regression can be compared numerically.
    /// <list type="bullet">
    /// <item><see cref="Calm"/> — guards patrol normally (level &lt; 20).</item>
    /// <item><see cref="Stirred"/> — guards investigate noise sources; alarm decays if quiet (20–50).</item>
    /// <item><see cref="Roused"/> — all guards alert, doors lock; alarm no longer decays (50–80).</item>
    /// <item><see cref="HueAndCry"/> — everyone converges, reinforcements spawn; permanent (≥ 80).</item>
    /// </list>
    /// </summary>
    public enum AlarmState
    {
        Calm = 0,
        Stirred = 1,
        Roused = 2,
        HueAndCry = 3
    }
}

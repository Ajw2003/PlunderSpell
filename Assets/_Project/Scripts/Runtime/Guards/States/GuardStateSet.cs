using Plunderspell.Status;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The states one guard can be in, made once so a guard moves between instances instead of
    /// allocating a state per change. Each behaviour issue (#207-#213) swaps its placeholder here.
    /// </summary>
    public sealed class GuardStateSet
    {
        public GuardStateSet(Guard guard)
        {
            Patrol = new PatrolState(guard);
            Investigate = new InvestigateState(guard);
            Chase = new ChaseState(guard);
            Combat = new CombatState(guard);
            Stunned = new StunnedState(guard);
            Slept = new SleptState(guard);
            Dead = new PlaceholderDeadState(guard);
        }

        /// <summary>Walking about (#207).</summary>
        public GuardState Patrol { get; }

        /// <summary>Going to look at a noise, a sighting or the hue and cry (#208).</summary>
        public GuardState Investigate { get; }

        /// <summary>Following a player who was seen (#209).</summary>
        public ChaseState Chase { get; }

        /// <summary>Fighting a player in reach, taking turns (#210).</summary>
        public CombatState Combat { get; }

        /// <summary>Stunned, or levitated until it lands (#211).</summary>
        public GuardState Stunned { get; }

        /// <summary>Asleep until the sleep ends or a loud noise wakes it (#211).</summary>
        public GuardState Slept { get; }

        /// <summary>The state for a guard a status effect has just incapacitated. A stun or levitation
        /// outranks sleep, because it holds the guard for longer than a noise can wake it.</summary>
        public GuardState IncapacitatedBy(StatusEffectReceiver status)
        {
            return status.IsStunned || status.IsLevitating ? Stunned : Slept;
        }

        /// <summary>Down for good (#213).</summary>
        public GuardState Dead { get; }
    }
}

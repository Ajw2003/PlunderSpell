using System;
using UnityEngine;

namespace Plunderspell.Guards
{
    /// <summary>
    /// The numbers of a guard that turns into the Wick (#417). Off on every guard but the Lantern Warden.
    /// <see cref="BecomeWick"/> writes the Wick's speeds, sight and catch into the guard's own tuning.
    /// </summary>
    [Serializable]
    public sealed class TrueFormTuning
    {
        [Tooltip("This guard turns into the Wick, one way, and is never killed. Only the Lantern Warden has it.")]
        public bool CanTurn;

        [Tooltip("How long the turn holds the guard still: warden_turn (0.9 s) then wick_emerge (0.7 s).")]
        public float TurnSeconds = 1.6f;

        [Tooltip("When the Wick's model replaces the warden's, in seconds from the start of the turn.")]
        public float SwapSeconds = 0.9f;

        [Tooltip("Stalking speed, in metres per second (wick_stalk).")]
        public float StalkSpeed = 1.4f;

        [Tooltip("Hunting speed, in metres per second (wick_hunt).")]
        public float HuntSpeed = 4.2f;

        [Tooltip("The Wick sees only inside its own fire's light: this far, all the way round, in metres.")]
        public float LightRadius = 6f;

        [Tooltip("A catch takes this much health: more than any player has.")]
        public float CatchDamage = 100000f;

        [Tooltip("A blow to the head counts only above this height over the guard's feet, in metres.")]
        public float HeadHeight = 1.5f;

        [Tooltip("A blow to the head stuns only at this damage or more.")]
        public float HardBlowDamage = 20f;

        [Tooltip("How long a hard blow to the head holds the human form down, in seconds.")]
        public float HeadStunSeconds = 6f;

        /// <summary>Writes the Wick's numbers over <paramref name="tuning"/>.</summary>
        public void BecomeWick(GuardTuning tuning)
        {
            tuning.PatrolSpeed = StalkSpeed;
            tuning.InvestigateSpeed = StalkSpeed;
            tuning.ChaseSpeed = HuntSpeed;
            tuning.FieldOfView = 360f;
            tuning.AttackDamage = CatchDamage;
        }
    }
}

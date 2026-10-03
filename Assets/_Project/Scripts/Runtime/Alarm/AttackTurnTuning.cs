using System;
using UnityEngine;

namespace Plunderspell.Alarm
{
    /// <summary>
    /// How many guards may attack one player at once (#210). The numbers come from the issue (1 melee and
    /// 1 ranged per target): the legacy guard had no limit and no turns, so every guard in reach struck.
    /// </summary>
    [Serializable]
    public sealed class AttackTurnTuning
    {
        [Tooltip("Melee guards that may strike one player at the same time.")]
        public int MeleeTurnsPerTarget = 1;

        [Tooltip("Ranged guards that may shoot one player at the same time.")]
        public int RangedTurnsPerTarget = 1;

        [Tooltip("A turn the holder has not used by now is taken back, in seconds. Stops a guard that cannot get to the player from blocking the others.")]
        public float TurnTimeoutSeconds = 3f;
    }
}

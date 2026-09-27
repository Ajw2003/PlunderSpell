using System;
using Plunderspell.Alarm;
using Plunderspell.Inventory;
using UnityEngine;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// The castle's night in its four alarm states (docs/plans/night-atmosphere.md, section 2):
    /// calm and asleep, then warmer, brighter and redder as the alarm climbs.
    /// </summary>
    [CreateAssetMenu(menuName = "Plunderspell/Night Atmosphere Profile", fileName = "NightAtmosphere")]
    public class NightAtmosphereProfile : ScriptableObject
    {
        [Tooltip("Seconds to ease from one state's look to the next.")]
        public float TransitionSeconds = 2f;

        public AtmosphereLook Calm;
        public AtmosphereLook Stirred;
        public AtmosphereLook Roused;
        public AtmosphereLook HueAndCry;

        /// <summary>What shifts from one Age to the next: only the stone and the flame (section 1, rule 6).</summary>
        [Serializable]
        public struct EraTint
        {
            [Tooltip("Multiplies stone albedo at night: darker and warmer, so fire reads against it.")]
            public Color Stone;
            [Tooltip("Multiplies the flame colour.")]
            public Color Flame;
        }

        public EraTint BronzeAge;
        public EraTint HighMedieval;
        public EraTint LateMedieval;
        public EraTint AgeOfPowder;

        /// <summary>The tints for one Age.</summary>
        public EraTint ForEra(HistoricalEra era)
        {
            switch (era)
            {
                case HistoricalEra.BronzeAge: return BronzeAge;
                case HistoricalEra.LateMedieval: return LateMedieval;
                case HistoricalEra.AgeOfPowder: return AgeOfPowder;
                default: return HighMedieval;
            }
        }

        /// <summary>The look for one alarm state.</summary>
        public AtmosphereLook For(AlarmState state)
        {
            switch (state)
            {
                case AlarmState.Stirred: return Stirred;
                case AlarmState.Roused: return Roused;
                case AlarmState.HueAndCry: return HueAndCry;
                default: return Calm;
            }
        }
    }
}

using RogueAi.Alarm;
using UnityEngine;

namespace RogueAi.Atmosphere
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

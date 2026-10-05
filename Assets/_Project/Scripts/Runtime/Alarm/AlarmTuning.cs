namespace Plunderspell.Alarm
{
    /// <summary>
    /// The numbers the alarm scores and decays by, copied from the director's Inspector fields when the
    /// alarm is first used. The fields stay on the director so the scenes that set them keep their values.
    /// </summary>
    public readonly struct AlarmTuning
    {
        public readonly float NoiseWeight;
        public readonly float DecayDelay;
        public readonly float DecayRate;
        public readonly float SightingPoints;
        public readonly float AttackPoints;
        public readonly int RousedChasers;
        public readonly int HueAndCryChasers;
        public readonly int RousedWitnesses;
        public readonly int HueAndCryWitnesses;

        public AlarmTuning(float noiseWeight, float decayDelay, float decayRate, float sightingPoints,
            float attackPoints, int rousedChasers, int hueAndCryChasers, int rousedWitnesses, int hueAndCryWitnesses)
        {
            NoiseWeight = noiseWeight;
            DecayDelay = decayDelay;
            DecayRate = decayRate;
            SightingPoints = sightingPoints;
            AttackPoints = attackPoints;
            RousedChasers = rousedChasers;
            HueAndCryChasers = hueAndCryChasers;
            RousedWitnesses = rousedWitnesses;
            HueAndCryWitnesses = hueAndCryWitnesses;
        }
    }
}

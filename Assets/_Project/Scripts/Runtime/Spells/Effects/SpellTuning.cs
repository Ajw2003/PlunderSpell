using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Every number that decides how a cast feels, read from the authored
    /// <see cref="SpellTuningProfile"/> asset (#105) — edit <c>Assets/_Project/Resources/SpellTuning.asset</c>,
    /// not this file.
    ///
    /// The central trade-off of Plunderspell lives in that asset: <see cref="CastVolume"/> scales a
    /// spell's power AND the noise it makes, in the same direction. A whisper is weak and nearly
    /// silent; a shout is strong and wakes the castle.
    /// </summary>
    public static class SpellTuning
    {
        /// <summary>Where the live asset is loaded from (a Resources path, so it ships in every build).</summary>
        public const string ResourcePath = "SpellTuning";

        private static SpellTuningProfile _profile;

        /// <summary>
        /// The profile in use: one set with <see cref="Use"/>, else the Resources asset, else the
        /// built-in defaults (the values the game had as constants).
        /// </summary>
        public static SpellTuningProfile Profile
        {
            get
            {
                if (_profile == null)
                    _profile = Resources.Load<SpellTuningProfile>(ResourcePath);
                if (_profile == null)
                    _profile = ScriptableObject.CreateInstance<SpellTuningProfile>();
                return _profile;
            }
        }

        /// <summary>Swaps the profile — for tests and balance experiments. Null reverts to the asset.</summary>
        public static void Use(SpellTuningProfile profile) => _profile = profile;

        public static float PowerMultiplier(CastVolume volume) => Profile.PowerMultiplier(volume);
        public static float NoiseRadius(CastVolume volume) => Profile.NoiseRadius(volume);
        public static float NoiseStrength(CastVolume volume) => Profile.NoiseStrength(volume);

        public static float DefaultEffectRadius => Profile.DefaultEffectRadius;
        public static float AimRange => Profile.AimRange;
        public static float AimConeDegrees => Profile.AimConeDegrees;
        public static float IgnisDamagePerSecond => Profile.IgnisDamagePerSecond;
        public static float IgnisBurnSeconds => Profile.IgnisBurnSeconds;
        public static float TonitrusStunSeconds => Profile.TonitrusStunSeconds;
        public static float TonitrusNoiseRadius => Profile.TonitrusNoiseRadius;
        public static float TonitrusNoiseStrength => Profile.TonitrusNoiseStrength;
        public static float SomnusSleepSeconds => Profile.SomnusSleepSeconds;
        public static float LevoImpulse => Profile.LevoImpulse;
        public static float LevoSeconds => Profile.LevoSeconds;
        public static float AurumVocoWorth => Profile.AurumVocoWorth;
        public static float AurumVocoNoiseRadius => Profile.AurumVocoNoiseRadius;
        public static float AurumVocoNoiseStrength => Profile.AurumVocoNoiseStrength;
        public static float MisfireSelfBurnSeconds => Profile.MisfireSelfBurnSeconds;
        public static float MisfireSelfDamagePerSecond => Profile.MisfireSelfDamagePerSecond;
        public static float MisfireSelfStunSeconds => Profile.MisfireSelfStunSeconds;
        public static float MisfireSelfSleepSeconds => Profile.MisfireSelfSleepSeconds;
        public static float MisfireRadius => Profile.MisfireRadius;
    }
}

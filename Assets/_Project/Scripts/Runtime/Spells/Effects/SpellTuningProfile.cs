using RogueAi.Voice;
using UnityEngine;

namespace RogueAi.Spells
{
    /// <summary>
    /// Every number that decides how a cast feels, as an asset a designer edits in the Inspector
    /// (#105). The live one is <c>Assets/_Project/Resources/SpellTuning.asset</c>; <see cref="SpellTuning"/>
    /// reads it. The defaults below are the values the game shipped with while they were constants,
    /// so a missing asset changes nothing.
    /// </summary>
    [CreateAssetMenu(fileName = "SpellTuning", menuName = "Plunderspell/Spell Tuning")]
    public class SpellTuningProfile : ScriptableObject
    {
        [Header("The volume dial: power and noise move together")]
        [Tooltip("Power, damage and radius multiplier for a whispered cast.")]
        public float WhisperPower = 0.5f;
        [Tooltip("Power multiplier at normal speaking volume.")]
        public float NormalPower = 1.0f;
        [Tooltip("Power multiplier for a shouted cast.")]
        public float ShoutPower = 1.75f;

        [Tooltip("How far the noise of a whispered cast carries, in metres.")]
        public float WhisperNoiseRadius = 0.5f;
        public float NormalNoiseRadius = 5.0f;
        public float ShoutNoiseRadius = 12.0f;

        [Tooltip("Loudness (0..1) of a whispered cast's noise, before walls muffle it.")]
        [Range(0f, 1f)] public float WhisperNoiseStrength = 0.1f;
        [Range(0f, 1f)] public float NormalNoiseStrength = 0.45f;
        [Range(0f, 1f)] public float ShoutNoiseStrength = 1.0f;

        [Header("Mana")]
        [Tooltip("Mana regained per second, all the time. Each spell's cost is on its SpellWord " +
                 "asset (Assets/_Project/Data/Spells/).")]
        [Min(0f)] public float ManaRegenPerSecond = 2.5f;

        [Header("Keyboard casting")]
        [Tooltip("Seconds a number-key cast is chanted before it fires. Longer than holding V and " +
                 "saying the word, so keys are a slight disadvantage, not a speed advantage (#116).")]
        [Min(0f)] public float KeyboardCastSeconds = 1.5f;

        [Header("All spells")]
        [Tooltip("Base effect radius in metres, before the volume multiplier.")]
        public float DefaultEffectRadius = 6f;

        [Header("Aiming")]
        [Tooltip("How far an aimed spell reaches, in metres, before the volume multiplier.")]
        public float AimRange = 14f;
        [Tooltip("How far off the crosshair (degrees) a target can be and still be picked.")]
        [Range(1f, 60f)] public float AimConeDegrees = 22f;

        [Header("Ignis — fire")]
        [Tooltip("Damage per second while burning, before the volume multiplier.")]
        public float IgnisDamagePerSecond = 12f;
        public float IgnisBurnSeconds = 4f;

        [Header("Frango — force blast")]
        [Tooltip("Damage to the creature you aim at, before the volume multiplier.")]
        public float FrangoDamage = 30f;
        [Tooltip("How long the blast staggers them (they cannot act).")]
        public float FrangoStaggerSeconds = 1.2f;
        [Tooltip("How far it shoves them back, in metres, before the volume multiplier.")]
        public float FrangoKnockback = 2.5f;

        [Header("Tonitrus — thunderclap")]
        public float TonitrusStunSeconds = 3f;
        [Tooltip("The thunderclap's own noise, on top of the cast's.")]
        public float TonitrusNoiseRadius = 18f;
        [Range(0f, 1f)] public float TonitrusNoiseStrength = 1f;

        [Header("Somnus — sleep")]
        [Tooltip("How long a guard sleeps. Whisper it, or the noise wakes them anyway.")]
        public float SomnusSleepSeconds = 8f;

        [Header("Levo — levitate")]
        [Tooltip("Upward impulse given to a levitated object.")]
        public float LevoImpulse = 6f;
        public float LevoSeconds = 3f;

        [Header("Aurum Voco — conjure gold")]
        [Tooltip("Coin value conjured, before the volume multiplier.")]
        public float AurumVocoWorth = 40f;
        public float AurumVocoNoiseRadius = 8f;
        [Range(0f, 1f)] public float AurumVocoNoiseStrength = 0.5f;

        [Header("Misfires — punishing on purpose")]
        public float MisfireSelfBurnSeconds = 6f;
        public float MisfireSelfDamagePerSecond = 8f;
        public float MisfireSelfStunSeconds = 4f;
        public float MisfireSelfSleepSeconds = 5f;
        [Tooltip("How far a misfire looks for its (wrong) victim; tighter than an intended cast.")]
        public float MisfireRadius = 4f;

        public float PowerMultiplier(CastVolume volume) =>
            volume == CastVolume.Whisper ? WhisperPower : volume == CastVolume.Shout ? ShoutPower : NormalPower;

        public float NoiseRadius(CastVolume volume) =>
            volume == CastVolume.Whisper ? WhisperNoiseRadius : volume == CastVolume.Shout ? ShoutNoiseRadius : NormalNoiseRadius;

        public float NoiseStrength(CastVolume volume) =>
            volume == CastVolume.Whisper ? WhisperNoiseStrength : volume == CastVolume.Shout ? ShoutNoiseStrength : NormalNoiseStrength;
    }
}

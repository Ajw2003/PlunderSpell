using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plunderspell.Atmosphere
{
    /// <summary>
    /// Everything the night looks like in one alarm state: fog, moon, ambient, sky, how the fires
    /// burn, and the post-processing grade. <see cref="CastleAtmosphere"/> blends four of these.
    /// </summary>
    [Serializable]
    public struct AtmosphereLook
    {
        [Header("Fog")]
        [Tooltip("The colour of the night mist, and what far-away walls fade into. Dark blue is a cold night; " +
                 "grey-brown is smoky. Brighter values make the mist glow.")]
        [ColorUsage(false, true)] public Color FogColor;
        [Tooltip("How thick the night mist is near the ground. Higher hides far walls sooner; 0 is no mist. " +
                 "Typical values are 0.005 to 0.08. (The Overall fog amount multiplies this.)")]
        public float FogDensity;
        [Tooltip("Height in metres where the mist is thickest. Above this it thins out. Try 0 to 5.")]
        public float FogBaseHeight;
        [Tooltip("How quickly the mist thins as you go up. Higher: towers rise out of the mist sooner and the " +
                 "mist hugs the ground; lower: the mist reaches high up. Try 0.02 to 0.5.")]
        public float FogHeightFalloff;
        [Tooltip("How far away the sky counts as, in metres, when working out how misty it looks. Larger makes " +
                 "the sky more hidden by mist; smaller shows it clearer. Try 100 to 1000.")]
        public float SkyDistance;
        [Tooltip("How clear the sky is when you look up: 0 keeps it fogged over, 1 shows stars and moon clear " +
                 "overhead. Only affects the sky, not walls and rooms.")]
        [Range(0f, 1f)] public float SkyClarity;

        [Header("Moon")]
        [Tooltip("The colour of the moon and moonlight. Pale blue is a cold night; yellower is a harvest moon. " +
                 "Brighter values make a stronger moon in the sky.")]
        public Color MoonColor;
        [Tooltip("How strongly the moon lights the scene. It is only a faint fill that picks out shapes, never " +
                 "a floodlight. 0 is no moonlight. Try 0.05 to 0.5.")]
        public float MoonIntensity;
        [Tooltip("How much the mist glows with moonlight. Higher makes a silvery haze; 0 removes it. Try 0 to 2.")]
        public float MoonScatter;
        [Tooltip("How much the moon's haze gathers around the moon itself instead of spreading evenly. 0 is an " +
                 "even haze everywhere; towards 1 it is a tight glow when you look at the moon.")]
        [Range(0f, 0.95f)] public float MoonAnisotropy;

        [Header("Ambient and sky")]
        [Tooltip("Background light that comes from above: the dim fill on roofs and the tops of things. " +
                 "Brighter lifts the shadows from overhead.")]
        public Color AmbientSky;
        [Tooltip("Background light from the sides: the dim fill on upright walls. Brighter makes walls in " +
                 "shadow easier to see.")]
        public Color AmbientEquator;
        [Tooltip("Background light bounced up from the floor: the dim fill under ledges and arches. " +
                 "Darker makes undersides gloomier.")]
        public Color AmbientGround;
        [Tooltip("The colour of the night sky straight overhead.")]
        public Color SkyZenith;
        [Tooltip("The colour of the night sky at the horizon, where it meets the walls and mist.")]
        public Color SkyHorizon;

        [Header("Fire")]
        [Tooltip("The colour of flames, and of the light they cast. Orange is a torch; whiter is a hotter fire. " +
                 "Brighter values make a brighter flame.")]
        [ColorUsage(false, true)] public Color FlameColor;
        [Tooltip("How bright every fire's light is on the walls. 0 puts the fires out of the lighting; higher " +
                 "lights more of the room. Try 0.5 to 3.")]
        public float FireIntensity;
        [Tooltip("How strongly fire lights the mist around it: the glowing halo round each flame. 0 is no halo. " +
                 "(The Overall fire glow amount multiplies this.)")]
        public float FireScatter;
        [Tooltip("How much a fire's halo shows when you look towards the fire instead of spreading evenly. 0 is " +
                 "an even glow; towards 1 the glow is bright only when you face the flame.")]
        [Range(0f, 0.95f)] public float FireAnisotropy;

        [Header("Grade")]
        [Tooltip("The colour grade for this state: the screen-wide tint, contrast, bloom and film grain. " +
                 "Open the asset to change them. Blends smoothly between states.")]
        public VolumeProfile Post;

        /// <summary>Adds <paramref name="look"/> scaled by <paramref name="weight"/> into this one (not the profile).</summary>
        public void Accumulate(in AtmosphereLook look, float weight)
        {
            FogColor += look.FogColor * weight;
            FogDensity += look.FogDensity * weight;
            FogBaseHeight += look.FogBaseHeight * weight;
            FogHeightFalloff += look.FogHeightFalloff * weight;
            SkyDistance += look.SkyDistance * weight;
            SkyClarity += look.SkyClarity * weight;
            MoonColor += look.MoonColor * weight;
            MoonIntensity += look.MoonIntensity * weight;
            MoonScatter += look.MoonScatter * weight;
            MoonAnisotropy += look.MoonAnisotropy * weight;
            AmbientSky += look.AmbientSky * weight;
            AmbientEquator += look.AmbientEquator * weight;
            AmbientGround += look.AmbientGround * weight;
            SkyZenith += look.SkyZenith * weight;
            SkyHorizon += look.SkyHorizon * weight;
            FlameColor += look.FlameColor * weight;
            FireIntensity += look.FireIntensity * weight;
            FireScatter += look.FireScatter * weight;
            FireAnisotropy += look.FireAnisotropy * weight;
        }
    }
}

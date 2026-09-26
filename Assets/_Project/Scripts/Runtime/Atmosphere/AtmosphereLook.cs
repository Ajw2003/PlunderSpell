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
        [Tooltip("Fog lit by the night's ambient light: what distance fades to.")]
        [ColorUsage(false, true)] public Color FogColor;
        [Tooltip("Fog density at the base height, per metre.")]
        public float FogDensity;
        [Tooltip("Height (m) the fog is densest at.")]
        public float FogBaseHeight;
        [Tooltip("How fast the fog thins with height, per metre. Higher: towers rise out of it sooner.")]
        public float FogHeightFalloff;
        [Tooltip("How far away the sky counts as, in metres of fog.")]
        public float SkyDistance;

        [Header("Moon")]
        public Color MoonColor;
        [Tooltip("Directional light intensity. A faint fill: it separates silhouettes, never lights a scene.")]
        public float MoonIntensity;
        [Tooltip("How much moonlight the fog scatters towards the camera.")]
        public float MoonScatter;
        [Range(0f, 0.95f)] public float MoonAnisotropy;

        [Header("Ambient and sky")]
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        public Color SkyZenith;
        public Color SkyHorizon;

        [Header("Fire")]
        [Tooltip("Flame colour, and the colour fires light with.")]
        [ColorUsage(false, true)] public Color FlameColor;
        [Tooltip("Multiplies every fire's light.")]
        public float FireIntensity;
        [Tooltip("How strongly fire lights the fog around it: the halo.")]
        public float FireScatter;
        [Range(0f, 0.95f)] public float FireAnisotropy;

        [Header("Grade")]
        [Tooltip("Post-processing for this state. Blended by volume weight.")]
        public VolumeProfile Post;

        /// <summary>Adds <paramref name="look"/> scaled by <paramref name="weight"/> into this one (not the profile).</summary>
        public void Accumulate(in AtmosphereLook look, float weight)
        {
            FogColor += look.FogColor * weight;
            FogDensity += look.FogDensity * weight;
            FogBaseHeight += look.FogBaseHeight * weight;
            FogHeightFalloff += look.FogHeightFalloff * weight;
            SkyDistance += look.SkyDistance * weight;
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

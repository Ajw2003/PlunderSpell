using Plunderspell.Atmosphere;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Plunderspell.EditorTools
{
    /// <summary>
    /// The tuned numbers for the night's four states, written into the profile by
    /// <see cref="NightAtmosphereForge"/>. Started from the chosen Blender renders
    /// (Tools/LookSamples/render_look_samples.py, LOOKS["calm"] and ["alert"]) and tuned against
    /// in-engine captures; see docs/4-systems/atmosphere.md ("Tuning").
    /// </summary>
    public static class NightLooks
    {
        // Night stone per Age: each kit's stone pigment pulled darker and warmer. High Medieval's is
        // the cool grey "iron", so it is warmed more than darkened; the others build in pale
        // limestone, sandstone and plaster, so they are darkened more.
        public static readonly NightAtmosphereProfile.EraTint HighMedievalTint = new NightAtmosphereProfile.EraTint
            { Stone = new Color(1.12f, 0.97f, 0.82f), Flame = Color.white };
        public static readonly NightAtmosphereProfile.EraTint BronzeAgeTint = new NightAtmosphereProfile.EraTint
            { Stone = new Color(0.72f, 0.62f, 0.52f), Flame = new Color(1f, 1.04f, 1.08f) };
        public static readonly NightAtmosphereProfile.EraTint LateMedievalTint = new NightAtmosphereProfile.EraTint
            { Stone = new Color(0.74f, 0.64f, 0.54f), Flame = Color.white };
        public static readonly NightAtmosphereProfile.EraTint AgeOfPowderTint = new NightAtmosphereProfile.EraTint
            { Stone = new Color(0.66f, 0.6f, 0.54f), Flame = new Color(1f, 0.97f, 0.92f) };

        public static AtmosphereLook Calm(VolumeProfile post) => new AtmosphereLook
        {
            FogColor = new Color(0.15f, 0.10f, 0.068f),
            FogDensity = 0.038f,
            FogBaseHeight = 0f,
            FogHeightFalloff = 0.075f,
            SkyDistance = 140f,
            MoonColor = new Color(0.45f, 0.58f, 0.9f),
            MoonIntensity = 0.035f,
            MoonScatter = 0.02f,
            MoonAnisotropy = 0.55f,
            AmbientSky = new Color(0.075f, 0.072f, 0.08f),
            AmbientEquator = new Color(0.12f, 0.085f, 0.06f),
            AmbientGround = new Color(0.07f, 0.05f, 0.036f),
            SkyZenith = new Color(0.012f, 0.012f, 0.016f),
            SkyHorizon = new Color(0.05f, 0.04f, 0.035f),
            FlameColor = new Color(1.0f, 0.5f, 0.18f),
            FireIntensity = 1.4f,
            FireScatter = 1.9f,
            FireAnisotropy = 0.6f,
            Post = post,
        };

        public static AtmosphereLook Stirred(VolumeProfile post)
        {
            AtmosphereLook look = Calm(post);
            look.FogColor = new Color(0.17f, 0.11f, 0.07f);
            look.FlameColor = new Color(1.0f, 0.49f, 0.18f);
            look.FireIntensity = 1.5f;
            look.FireScatter = 2.1f;
            look.SkyHorizon = new Color(0.06f, 0.045f, 0.035f);
            return look;
        }

        public static AtmosphereLook Roused(VolumeProfile post)
        {
            AtmosphereLook look = Calm(post);
            look.FogColor = new Color(0.22f, 0.12f, 0.06f);
            look.FogDensity = 0.034f;
            look.FlameColor = new Color(1.0f, 0.42f, 0.13f);
            look.FireIntensity = 1.5f;
            look.FireScatter = 2.3f;
            look.SkyHorizon = new Color(0.09f, 0.05f, 0.03f);
            look.AmbientEquator = new Color(0.15f, 0.09f, 0.055f);
            return look;
        }

        public static AtmosphereLook HueAndCry(VolumeProfile post)
        {
            AtmosphereLook look = Calm(post);
            look.FogColor = new Color(0.25f, 0.1f, 0.05f);
            look.FogDensity = 0.030f;
            look.FlameColor = new Color(1.0f, 0.34f, 0.10f);
            look.FireIntensity = 1.55f;
            look.FireScatter = 2.4f;
            look.SkyZenith = new Color(0.03f, 0.012f, 0.01f);
            look.SkyHorizon = new Color(0.12f, 0.045f, 0.025f);
            look.AmbientEquator = new Color(0.17f, 0.08f, 0.05f);
            look.AmbientGround = new Color(0.08f, 0.04f, 0.028f);
            return look;
        }

        public static void CalmGrade(VolumeProfile p) => Grade(p, exposure: 0.55f, contrast: 10f, saturation: -4f,
            filter: new Color(1.04f, 1.0f, 0.93f), bloom: 0.9f, vignette: 0.33f,
            lift: new Vector4(0.99f, 0.99f, 1.02f, 0f), gain: new Vector4(1.05f, 1.0f, 0.92f, 0f));

        public static void StirredGrade(VolumeProfile p) => Grade(p, exposure: 0.55f, contrast: 14f, saturation: -2f,
            filter: new Color(1.05f, 0.99f, 0.91f), bloom: 1.05f, vignette: 0.36f,
            lift: new Vector4(1.0f, 0.99f, 1.0f, 0f), gain: new Vector4(1.06f, 0.99f, 0.9f, 0f));

        public static void RousedGrade(VolumeProfile p) => Grade(p, exposure: 0.35f, contrast: 20f, saturation: 2f,
            filter: new Color(1.07f, 0.97f, 0.88f), bloom: 1.3f, vignette: 0.4f,
            lift: new Vector4(1.0f, 0.98f, 0.98f, 0f), gain: new Vector4(1.08f, 0.98f, 0.88f, 0f));

        public static void HueAndCryGrade(VolumeProfile p) => Grade(p, exposure: 0.2f, contrast: 30f, saturation: 6f,
            filter: new Color(1.1f, 0.94f, 0.85f), bloom: 1.6f, vignette: 0.46f,
            lift: new Vector4(1.02f, 0.96f, 0.95f, 0f), gain: new Vector4(1.1f, 0.96f, 0.85f, 0f));

        private static void Grade(VolumeProfile profile, float exposure, float contrast, float saturation, Color filter,
            float bloom, float vignette, Vector4 lift, Vector4 gain)
        {
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.ACES);

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(exposure);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);
            color.colorFilter.Override(filter);

            // The warm split: amber highlights, a cool moon edge in the shadows.
            SplitToning split = profile.Add<SplitToning>(true);
            split.shadows.Override(new Color(0.48f, 0.5f, 0.54f));
            split.highlights.Override(new Color(0.74f, 0.6f, 0.46f));
            split.balance.Override(10f);

            LiftGammaGain lgg = profile.Add<LiftGammaGain>(true);
            lgg.lift.Override(lift);
            lgg.gain.Override(gain);

            Bloom glow = profile.Add<Bloom>(true);
            glow.threshold.Override(0.85f);
            glow.intensity.Override(bloom);
            glow.scatter.Override(0.72f);
            glow.highQualityFiltering.Override(false);

            Vignette edge = profile.Add<Vignette>(true);
            edge.intensity.Override(vignette);
            edge.smoothness.Override(0.45f);
            edge.color.Override(new Color(0.02f, 0.01f, 0.005f));

            // Switched off below High by CastleAtmosphere.ApplyQuality.
            FilmGrain grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.intensity.Override(0.12f);
            grain.response.Override(0.8f);
        }
    }
}

// Play mode only. Scratch tuning: edits the live profile's Calm look and every fire's light so a
// capture can be compared with the Blender render. The forge (NightLooks.cs) is the source of
// truth; values that work are copied there by hand.
var atmos = Plunderspell.Atmosphere.CastleAtmosphere.Instance;
var so = typeof(Plunderspell.Atmosphere.CastleAtmosphere).GetField("_profile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
var profile = (Plunderspell.Atmosphere.NightAtmosphereProfile)so.GetValue(atmos);
var look = profile.Calm;
look.FireScatter = 14f;
look.FireIntensity = 2.2f;
look.MoonIntensity = 0.035f;
look.MoonScatter = 0.02f;
look.FogColor = new UnityEngine.Color(0.05f, 0.032f, 0.022f);
look.AmbientSky = new UnityEngine.Color(0.018f, 0.016f, 0.018f);
look.AmbientEquator = new UnityEngine.Color(0.03f, 0.02f, 0.014f);
look.AmbientGround = new UnityEngine.Color(0.014f, 0.009f, 0.006f);
profile.Calm = look;
foreach (var f in Plunderspell.Atmosphere.FireSource.All)
{
    var t = typeof(Plunderspell.Atmosphere.FireSource);
    var range = t.GetField("_range", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    range.SetValue(f, (float)range.GetValue(f) * 1.5f);
}
return "tuned";

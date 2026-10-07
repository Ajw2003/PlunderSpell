// Play mode, the side it runs on. Checks that a fire's budget light fades in rather than popping (#196),
// for Tools/Unity/fire_fade_check.sh, which replaces the placeholder below.
//   pick     choose the farthest burning fire that holds no light (or, failing that, no shadow), remember it,
//            and print it; the script then parks the camera beside it
//   watch    record the chosen fire every frame for 400 frames (eval round trips are too slow for a 0.5 s fade)
//   read     print the recorded frames, one line each
//   sample   print the chosen fire's grant, light on/off, light share (intensity / CurrentIntensity),
//            shadows and shadow strength, with the game time
string action = "__ACTION__";
var fires = Plunderspell.Atmosphere.FireSource.All;
var cam = UnityEngine.Camera.main;
if (cam == null) return "no camera";
if (action == "pick")
{
    Plunderspell.Atmosphere.FireSource best = null;
    float bestSqr = -1f;
    bool wantNoLight = false;
    foreach (var f in fires)
        if (f.IsBurning && f.Light != null && f.Grant == Plunderspell.Atmosphere.FireRules.LightGrant.None) wantNoLight = true;
    foreach (var f in fires)
    {
        if (!f.IsBurning || f.Light == null) continue;
        bool candidate = wantNoLight
            ? f.Grant == Plunderspell.Atmosphere.FireRules.LightGrant.None
            : f.Grant != Plunderspell.Atmosphere.FireRules.LightGrant.Shadowed;
        float sqr = (f.GlowPosition - cam.transform.position).sqrMagnitude;
        if (candidate && sqr > bestSqr) { best = f; bestSqr = sqr; }
    }
    if (best == null) return "no candidate fire";
    System.AppDomain.CurrentDomain.SetData("fadeFire", best);
    var g = best.GlowPosition;
    return "picked " + best.name + " grant " + best.Grant + " at " + g.x.ToString("0.0") + " " + g.y.ToString("0.0") + " " + g.z.ToString("0.0")
        + " dist " + UnityEngine.Mathf.Sqrt(bestSqr).ToString("0") + " | " + fires.Count + " fires, watching for " + (wantNoLight ? "light" : "shadow");
}
var fire = System.AppDomain.CurrentDomain.GetData("fadeFire") as Plunderspell.Atmosphere.FireSource;
if (fire == null) return "nothing picked";
System.Func<string> Line = () =>
{
    float s0 = fire.CurrentIntensity > 0f ? fire.Light.intensity / fire.CurrentIntensity : 0f;
    return "t " + UnityEngine.Time.time.ToString("0.000") + " grant " + fire.Grant + " on " + fire.Light.enabled + " share " + s0.ToString("0.00")
        + " shadows " + fire.Light.shadows + " strength " + fire.Light.shadowStrength.ToString("0.00");
};
if (action == "watch")
{
    var frames = new System.Collections.Generic.List<string>();
    System.AppDomain.CurrentDomain.SetData("fadeFrames", frames);
    UnityEngine.Events.UnityAction record = null;
    record = () =>
    {
        frames.Add(Line());
        if (frames.Count >= 400) UnityEngine.Application.onBeforeRender -= record;
    };
    UnityEngine.Application.onBeforeRender += record;
    return "watching " + fire.name;
}
if (action == "read")
{
    var frames = System.AppDomain.CurrentDomain.GetData("fadeFrames") as System.Collections.Generic.List<string>;
    return frames == null ? "nothing recorded" : string.Join(System.Environment.NewLine, frames);
}
var light = fire.Light;
float share = fire.CurrentIntensity > 0f ? light.intensity / fire.CurrentIntensity : 0f;
return "t " + UnityEngine.Time.time.ToString("0.00") + " grant " + fire.Grant + " on " + light.enabled + " share " + share.ToString("0.00")
    + " shadows " + light.shadows + " strength " + light.shadowStrength.ToString("0.00");

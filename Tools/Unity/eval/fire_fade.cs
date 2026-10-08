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
if (action == "walk")
{
    // Solo (#354): flies the camera through the castle's fires at 8 m/s for 30 s, Calm for the first 8 s and then the
    // alarm raised to the hue and cry, recording every fire's light intensity, shadow strength and halo strength every frame.
    var all = new System.Collections.Generic.List<Plunderspell.Atmosphere.FireSource>(fires);
    if (all.Count < 2) return "too few fires: " + all.Count;
    var player = StateMachine.PlayerStateMachine.Local;
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = true;
    var camOffset = cam.transform.position - player.transform.position;
    // A greedy chain through the fires, nearest first from the one nearest the camera.
    var left = new System.Collections.Generic.List<Plunderspell.Atmosphere.FireSource>(all);
    var path = new System.Collections.Generic.List<UnityEngine.Vector3>();
    var here = cam.transform.position;
    while (left.Count > 0 && path.Count < 60)
    {
        int bi = 0; float bd = float.MaxValue;
        for (int i = 0; i < left.Count; i++) { float d = (left[i].GlowPosition - here).sqrMagnitude; if (d < bd) { bd = d; bi = i; } }
        here = left[bi].GlowPosition; path.Add(here); left.RemoveAt(bi);
    }
    var atmosphere = Plunderspell.Atmosphere.CastleAtmosphere.Instance;
    var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
    var posField = typeof(Plunderspell.Atmosphere.CastleAtmosphere).GetField("_lightPos", flags);
    var colField = typeof(Plunderspell.Atmosphere.CastleAtmosphere).GetField("_lightColor", flags);
    var director = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Alarm.EnemyDirector>();
    int nf = all.Count;
    var series = new System.Collections.Generic.List<float>[nf * 3];
    for (int i = 0; i < series.Length; i++) series[i] = new System.Collections.Generic.List<float>();
    var times = new System.Collections.Generic.List<float>();
    var dts = new System.Collections.Generic.List<float>();
    var states = new System.Collections.Generic.List<int>();
    System.AppDomain.CurrentDomain.SetData("fadeAll", all);
    System.AppDomain.CurrentDomain.SetData("fadeSeries", series);
    System.AppDomain.CurrentDomain.SetData("fadeTimes", times);
    System.AppDomain.CurrentDomain.SetData("fadeDts", dts);
    float t0 = UnityEngine.Time.time; int seg = 0; float along = 0f; bool raised = false;
    UnityEngine.Vector3 at = path[0];
    UnityEngine.Events.UnityAction rec = null;
    rec = () =>
    {
        float t = UnityEngine.Time.time - t0, dt = UnityEngine.Time.deltaTime;
        if (t > 30f) { UnityEngine.Application.onBeforeRender -= rec; System.AppDomain.CurrentDomain.SetData("fadeDone", true); return; }
        if (!raised && t > 8f) { director.SetAlarmLevel(100f); raised = true; }
        // Move along the chain, looping back if it runs out.
        along += 8f * dt;
        while (path.Count > 1)
        {
            var a = path[seg % path.Count]; var b = path[(seg + 1) % path.Count];
            float len = (b - a).magnitude;
            if (along < len) { at = a + (b - a) * (len > 0f ? along / len : 0f); player.FaceYaw(UnityEngine.Mathf.Atan2(b.x - a.x, b.z - a.z) * UnityEngine.Mathf.Rad2Deg); break; }
            along -= len; seg++;
        }
        var feet = at - camOffset;
        rb.position = feet; player.transform.position = feet;
        UnityEngine.Physics.SyncTransforms();
        if (player.IsAlive) typeof(StateMachine.PlayerStateMachine).GetField("_health", flags).SetValue(player, player.MaxHealth);

        var pos = (UnityEngine.Vector4[])posField.GetValue(atmosphere);
        var col = (UnityEngine.Vector4[])colField.GetValue(atmosphere);
        int count = (int)UnityEngine.Shader.GetGlobalVector("_NF_Scatter").w;
        for (int i = 0; i < nf; i++)
        {
            var f = all[i]; var l = f.Light;
            bool on = l != null && l.enabled;
            series[i * 3].Add(on ? l.intensity : 0f);
            series[i * 3 + 1].Add(on && l.shadows != UnityEngine.LightShadows.None ? l.shadowStrength : 0f);
            float halo = 0f; var g = f.GlowPosition;
            for (int k = 0; k < count; k++)
                if ((new UnityEngine.Vector3(pos[k].x, pos[k].y, pos[k].z) - g).sqrMagnitude < 0.0001f) { halo = UnityEngine.Mathf.Max(col[k].x, UnityEngine.Mathf.Max(col[k].y, col[k].z)); break; }
            series[i * 3 + 2].Add(halo);
        }
        times.Add(t); dts.Add(dt);
    };
    UnityEngine.Application.onBeforeRender += rec;
    return "walking " + nf + " fires along " + path.Count + " waypoints, state " + director.State;
}
if (action == "report")
{
    var all = System.AppDomain.CurrentDomain.GetData("fadeAll") as System.Collections.Generic.List<Plunderspell.Atmosphere.FireSource>;
    var series = System.AppDomain.CurrentDomain.GetData("fadeSeries") as System.Collections.Generic.List<float>[];
    var times = System.AppDomain.CurrentDomain.GetData("fadeTimes") as System.Collections.Generic.List<float>;
    var dts = System.AppDomain.CurrentDomain.GetData("fadeDts") as System.Collections.Generic.List<float>;
    if (all == null || times.Count < 100) return "nothing recorded";
    const float fade = 1f;          // the rule: a quantity may move its full value over one second
    const float flickerAllow = 0.05f; // of the full value, per frame: the flicker, plus the halo and occlusion fades overlapping
    string[] names = { "light intensity", "shadow strength", "halo strength" };
    var lines = new System.Text.StringBuilder();
    lines.AppendLine("frames " + times.Count + ", fires " + all.Count + ", " + times[times.Count - 1].ToString("0.0") + " s; rule: change per frame <= full x dt / " + fade + " s + " + flickerAllow + " x full");
    int bad = 0;
    for (int q = 0; q < 3; q++)
    {
        int violations = 0, firesHit = 0; float worst = 0f, worstJump = 0f, worstFull = 0f, worstAt = 0f, worstDt = 0f; string worstFire = "-";
        for (int i = 0; i < all.Count; i++)
        {
            var s = series[i * 3 + q];
            float full = 0f; foreach (float v in s) full = UnityEngine.Mathf.Max(full, v);
            if (full <= 0f) continue;
            bool hit = false;
            for (int k = 21; k < s.Count; k++)
            {
                if (dts[k] > 0.03f) continue; // a screenshot or Editor hitch, not a game frame; the shots in the script cause these
                float jump = UnityEngine.Mathf.Abs(s[k] - s[k - 1]);
                float allow = full * dts[k] / fade + flickerAllow * full;
                if (jump > allow) { violations++; hit = true; }
                if (jump / allow > worst) { worst = jump / allow; worstJump = jump; worstFull = full; worstFire = all[i].name; worstAt = times[k]; worstDt = dts[k]; }
            }
            if (hit) firesHit++;
        }
        bad += violations;
        lines.AppendLine(names[q] + ": " + violations + " frames over the rule on " + firesHit + " fires; worst " + worstFire + " at t " + worstAt.ToString("0.00") + " (frame " + (worstDt * 1000f).ToString("0.0") + " ms)"
            + " jumped " + worstJump.ToString("0.00") + " of full " + worstFull.ToString("0.00") + " in one frame (" + (worstFull > 0 ? (100f * worstJump / worstFull).ToString("0") : "0") + "% of full, " + worst.ToString("0.0") + "x the allowance)");
    }
    lines.AppendLine(bad == 0 ? "PASS" : "FAIL " + bad + " frame-jumps over the rule");
    return lines.ToString();
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

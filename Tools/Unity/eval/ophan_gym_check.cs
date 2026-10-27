// Run through coop_eval.sh with COOP_EVAL_FILE set to this file: one action of the Ophan Gym check (#455) on one side
// of a local co-op session in OphanGym.unity. Placing, grabbing and screenshots come from ophan_check.cs.
//   read          the gym as this machine sees it: seconds since the raid started, the Ophan's state, which piece
//                 carries the pillar and where, the local player's health, no-damage, time scale, guards left
//   call <method> run the gym method a key runs (on a client, where Input System cannot be compiled); press a
//                 real key on the Editor with ophan_gym_press.cs
//   pillarshot <path>  turn this machine's view to the pillar and save a screenshot (absolute path, forward slashes)
string action = "__ACTION__";
string arg = "__ARG__";

const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;

System.Type T(string name)
{
    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        var type = assembly.GetType(name);
        if (type != null) return type;
    }
    throw new System.Exception("no type " + name);
}

object Get(object target, string name)
{
    var type = target as System.Type ?? target.GetType();
    object instance = target is System.Type ? null : target;
    for (var t = type; t != null; t = t.BaseType)
    {
        var property = t.GetProperty(name, All);
        if (property != null) return property.GetValue(instance);
        var field = t.GetField(name, All);
        if (field != null) return field.GetValue(instance);
    }
    throw new System.Exception("no member " + name + " on " + type.Name);
}

UnityEngine.Object[] FindAll(string typeName) =>
    UnityEngine.Object.FindObjectsByType(T(typeName), UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);

UnityEngine.Component Gym() => UnityEngine.Object.FindFirstObjectByType(T("Plunderspell.Playtest.OphanGym")) as UnityEngine.Component;
UnityEngine.Component Pillar() =>
    UnityEngine.Object.FindFirstObjectByType(T("Plunderspell.Playtest.OphanGymBeacon")) as UnityEngine.Component;
string V(UnityEngine.Vector3 v) => v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1");

switch (action)
{
    case "read":
    {
        var gym = Gym();
        if (gym == null) return "no gym in this scene";
        float started = (float)Get(gym, "RaidStartedAt");
        var pillar = Pillar();
        var local = Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;
        string health = local == null ? "none" : ((float)Get(local, "CurrentHealth")).ToString("F0");
        return "raid " + (started < 0f ? "not started" : (UnityEngine.Time.time - started).ToString("F1") + " s")
            + "; ophan " + Get(gym, "OphanState")
            + "; pillar " + (pillar == null ? "none" : pillar.name + " at " + V(pillar.transform.position))
            + "; health " + health
            + "; untouchable " + Get(gym, "IsLocalPlayerUntouchable")
            + "; timescale " + UnityEngine.Time.timeScale.ToString("0.##")
            + "; guards " + FindAll("Plunderspell.Guards.Guard").Length;
    }

    case "call":
    {
        // The player build cannot compile Input System calls, so a client asks its gym directly (Summon,
        // ToggleLocalPlayerUntouchable, RemoveGuards, CycleTimeScale): the same method its key runs.
        var gym = Gym();
        gym.GetType().GetMethod(arg).Invoke(gym, null);
        return "called " + arg;
    }

    case "pillarshot":
    {
        // arg is "<path>@<metres up>": a camera that many metres above the local player's head, looking at the pillar
        // 20 m up, so the shot shows what a player sees of it from here (0) or from a wall top (about 10).
        var parts = arg.Split('@');
        var pillar = Pillar();
        var local = Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;
        if (pillar == null || local == null) return "no pillar or no local player";
        var camGo = new UnityEngine.GameObject("PillarShotCamera");
        var cam = camGo.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = 70f;
        cam.farClipPlane = 3000f;
        var rt = new UnityEngine.RenderTexture(1280, 720, 24);
        cam.targetTexture = rt;
        cam.transform.position = local.transform.position + UnityEngine.Vector3.up * (1.6f + float.Parse(parts[1]));
        UnityEngine.Vector3 top = pillar.transform.position + UnityEngine.Vector3.up * 20f;
        cam.transform.LookAt(top);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(parts[0], UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.RenderTexture.active = null;
        UnityEngine.Object.Destroy(camGo);
        rt.Release();
        return "pillar " + UnityEngine.Vector3.Distance(cam.transform.position, top).ToString("F0") + " m away; saved " + parts[0];
    }
}
return "unknown action " + action;

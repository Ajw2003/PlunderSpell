// Play mode only. One side of docs/generated/escape-portal/escape_check.sh (#556): runs in the Editor (host) and in the
// Development build (client), so it reaches the game only through reflection. The script replaces the placeholders.
//   read            the portal's state, where it stands, the escape room, the clock, the hum and the glow as this machine has them
//   clock <secs>    host: set the raid clock's time remaining
//   pinself         host: count the local player as standing in the portal (no teleport of the rigid body)
//   noguards        host: remove every guard
//   shot <path>     save a view of the portal from a camera 7 m off it (absolute path, forward slashes)
//   phase           the raid phase and the last raid's line as this machine has them
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

void Set(object target, string name, object value)
{
    var type = target as System.Type ?? target.GetType();
    object instance = target is System.Type ? null : target;
    for (var t = type; t != null; t = t.BaseType)
    {
        var field = t.GetField(name, All);
        if (field != null) { field.SetValue(instance, value); return; }
    }
    throw new System.Exception("no field " + name + " on " + type.Name);
}

object Call(object target, string name, params object[] args)
{
    var type = target as System.Type ?? target.GetType();
    object instance = target is System.Type ? null : target;
    for (var t = type; t != null; t = t.BaseType)
        foreach (var method in t.GetMethods(All))
            if (method.Name == name && method.GetParameters().Length == args.Length)
                return method.Invoke(instance, args);
    throw new System.Exception("no method " + name + "/" + args.Length + " on " + type.Name);
}

UnityEngine.Object[] FindAll(string typeName) =>
    UnityEngine.Object.FindObjectsByType(T(typeName), UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);

var director = FindAll("Plunderspell.Raid.RaidDirector")[0];
if (action == "phase")
{
    var gs = Get(T("Plunderspell.Core.GameServices"), "GameState");
    return "phase " + Get(director, "Phase") + ", game state " + gs.GetType().GetProperty("CurrentState").GetValue(gs)
        + ", raid scene " + (FindAll("Plunderspell.Extraction.ExtractionZone").Length > 0 ? "loaded" : "unloaded");
}
var zone = FindAll("Plunderspell.Extraction.ExtractionZone")[0];
var zt = ((UnityEngine.Component)zone).transform;

if (action == "read")
{
    object audio = Get(T("Plunderspell.Audio.AudioDirector"), "Instance");
    var hum = audio != null ? Get(audio, "_portalHum") as UnityEngine.AudioSource : null;
    var glow = FindAll("Plunderspell.Atmosphere.PortalGlow");
    string glowText = glow.Length == 0 ? "no glow" : "glow at " + ((UnityEngine.Component)glow[0]).transform.position.ToString("F1")
        + " open " + Get(glow[0], "_open");
    var box = ((UnityEngine.Component)zone).GetComponent<UnityEngine.Collider>();
    return "state " + Get(zone, "State") + " at " + zt.position.ToString("F1") + " trigger " + (box != null && box.enabled)
        + " | escape room " + Get(director, "EscapeModuleIndex") + " arrival room " + Get(director, "ArrivalModuleIndex")
        + " | remaining " + ((float)Get(zone, "TimeRemaining")).ToString("F1")
        + " | hum " + (hum == null ? "none" : (hum.isPlaying ? "playing" : "stopped") + " at " + hum.transform.position.ToString("F1") + " max " + hum.maxDistance)
        + " | " + glowText;
}
if (action == "clock")
{
    object sync = Get(zone, "_timeRemaining");
    sync.GetType().GetProperty("value").SetValue(sync, float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture));
    return "remaining now " + Get(zone, "TimeRemaining");
}
if (action == "pinself")
{
    var local = Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;
    var identity = local.GetComponent(T("PurrNet.NetworkIdentity"));
    Call(zone, "TrackPlayer", identity);
    return "pinned " + local.name + "; players in zone " + Get(zone, "PlayersInZoneCount");
}
if (action == "noguards")
{
    int n = 0;
    foreach (var g in FindAll("Plunderspell.Guards.Guard")) { UnityEngine.Object.Destroy(((UnityEngine.Component)g).gameObject); n++; }
    return "removed " + n + " guards";
}
if (action == "shot")
{
    var camGo = new UnityEngine.GameObject("EscapeShotCamera");
    var cam = camGo.AddComponent<UnityEngine.Camera>();
    // From inside the room: the first of eight directions with no stone between the camera and the portal.
    var centre = zt.position + UnityEngine.Vector3.up * 1.5f;
    var eye = centre + new UnityEngine.Vector3(0f, 0.5f, -3.5f);
    for (int i = 0; i < 8; i++)
    {
        float a = i * UnityEngine.Mathf.PI / 4f;
        var candidate = centre + new UnityEngine.Vector3(UnityEngine.Mathf.Sin(a) * 3.5f, 0.5f, UnityEngine.Mathf.Cos(a) * 3.5f);
        if (!UnityEngine.Physics.Linecast(centre, candidate, ~0, UnityEngine.QueryTriggerInteraction.Ignore)) { eye = candidate; break; }
    }
    cam.transform.position = eye;
    cam.transform.LookAt(zt.position + UnityEngine.Vector3.up * 1.5f);
    cam.fieldOfView = 70f;
    var rt = new UnityEngine.RenderTexture(1280, 720, 24);
    cam.targetTexture = rt;
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1280, 720, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1280, 720), 0, 0);
    tex.Apply();
    System.IO.File.WriteAllBytes(arg, UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.RenderTexture.active = null;
    cam.targetTexture = null;
    UnityEngine.Object.Destroy(rt);
    UnityEngine.Object.Destroy(camGo);
    return "saved " + arg;
}
return "unknown action " + action;

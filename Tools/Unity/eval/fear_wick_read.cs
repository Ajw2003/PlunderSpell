// Play mode only. Runs the same on the host (Editor) and the client (Development build, reflection only), for
// Tools/Unity/fear_wick_check.sh (#417). The script replaces the two placeholders below.
//   read         every Lantern Warden here: replicated state, turned flag, whether its Wick is showing, where it
//                is; the loose lanterns' positions; each player body's health
//   slow <scale> set Time.timeScale on this machine (1 restores)
//   face         turn this machine's player to look at the warden
//   shot <path>  save a screenshot of this machine's view (absolute path, forward slashes)
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
    for (var t = target.GetType(); t != null; t = t.BaseType)
    {
        var property = t.GetProperty(name, All);
        if (property != null) return property.GetValue(target);
        var field = t.GetField(name, All);
        if (field != null) return field.GetValue(target);
    }
    return "?";
}

if (action == "shot")
{
    UnityEngine.ScreenCapture.CaptureScreenshot(arg);
    return "shot requested " + arg;
}

if (action == "slow")
{
    // Slow motion on this machine, so a screenshot can catch the Wick before the catch lands.
    UnityEngine.Time.timeScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
    return "time scale " + UnityEngine.Time.timeScale;
}
if (action == "face")
{
    // Turn this machine's player to look at the warden, so no camera is left looking at the arrival portal.
    UnityEngine.Component warden = null;
    foreach (UnityEngine.Component g in UnityEngine.Object.FindObjectsByType(T("Plunderspell.Guards.Guard"), UnityEngine.FindObjectsSortMode.None))
        if (g.name.StartsWith("LanternWarden")) warden = g;
    var playerType = T("StateMachine.PlayerStateMachine");
    var me = (UnityEngine.Component)playerType.GetProperty("Local").GetValue(null);
    if (warden == null || me == null) return "no warden or no local player";
    var toward = warden.transform.position - me.transform.position; toward.y = 0f;
    playerType.GetMethod("FaceYaw").Invoke(me, new object[] { UnityEngine.Quaternion.LookRotation(toward).eulerAngles.y });
    return "facing the warden, " + toward.magnitude.ToString("F1") + " m away";
}

var sb = new System.Text.StringBuilder();
foreach (UnityEngine.Component guard in UnityEngine.Object.FindObjectsByType(T("Plunderspell.Guards.Guard"), UnityEngine.FindObjectsSortMode.None))
{
    if (!guard.name.StartsWith("LanternWarden")) continue;
    var wick = guard.transform.Find("TheWick");
    sb.Append(guard.name + " state=" + Get(guard, "State") + " turned=" + Get(guard, "IsTurned")
        + " wickShowing=" + (wick != null && wick.gameObject.activeSelf)
        + " at=" + guard.transform.position.ToString("F2") + " | ");
}
foreach (var body in UnityEngine.Object.FindObjectsByType<UnityEngine.Rigidbody>(UnityEngine.FindObjectsSortMode.None))
{
    if (body.name.StartsWith("FallenLantern")) sb.Append("lantern at " + body.position.ToString("F2") + " | ");
}
foreach (UnityEngine.Component player in UnityEngine.Object.FindObjectsByType(T("StateMachine.PlayerStateMachine"), UnityEngine.FindObjectsSortMode.None))
{
    sb.Append("player local=" + Get(player, "IsLocal") + " health=" + Get(player, "CurrentHealth") + " | ");
}
return sb.ToString();

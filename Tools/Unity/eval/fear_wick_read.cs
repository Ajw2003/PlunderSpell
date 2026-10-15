// Play mode only. Runs the same on the host (Editor) and the client (Development build, reflection only), for
// Tools/Unity/fear_wick_check.sh (#417). The script replaces the two placeholders below.
//   read         every Lantern Warden here: replicated state, turned flag, whether its Wick is showing, where it
//                is; the loose lanterns' positions; each player body's health
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

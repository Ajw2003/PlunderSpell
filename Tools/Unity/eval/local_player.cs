// Play mode, either side: something done to this machine's own player, for guard_baseline_check.sh.
//   untouchable   turns the gym's no-damage on for this player, leaving it on if it already is (F8 is a toggle)
//   park x,y,z    holds this player still (kinematic) at that point, out of the way of a test
// Reaches the game only through reflection, since that is all a Development build's eval can do (the client side).
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

var p = T("StateMachine.PlayerStateMachine").GetProperty("Local", All).GetValue(null) as UnityEngine.Component;
if (p == null) return "no local player";

if (action == "untouchable")
{
    var gymType = T("Plunderspell.Playtest.OphanGym");
    var gym = UnityEngine.Object.FindFirstObjectByType(gymType);
    if (gym == null) return "no gym";
    var flag = gymType.GetProperty("IsLocalPlayerUntouchable", All);
    if (!(bool)flag.GetValue(gym)) gymType.GetMethod("ToggleLocalPlayerUntouchable", All).Invoke(gym, null);
    return "untouchable " + flag.GetValue(gym);
}

if (action == "park")
{
    string[] xyz = arg.Split(',');
    var c = System.Globalization.CultureInfo.InvariantCulture;
    var at = new UnityEngine.Vector3(float.Parse(xyz[0], c), float.Parse(xyz[1], c), float.Parse(xyz[2], c));
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = true;
    rb.position = at; p.transform.position = at;
    UnityEngine.Physics.SyncTransforms();
    return "parked at " + at;
}

return "unknown action " + action;

// Play mode only. One side of Tools/Unity/coop_door_check.sh (#248): runs the same in the Editor (host) and
// the Development build (client), so it reaches the game only through reflection. Doors are numbered by
// position (x, then z), which is the same on both sides because the castle is built from the seed.
//   doors       count, how many are open, locked, barred
//   door <n>    door n: open, locked, barred, hinge yaw, position
//   hand <n>    push door n by hand, as the interact key does (its CastleDoorHandle)
//   porta <n>   open door n the way Porta does (IOpenable.Open)
//   lockdown    host: lock every door, as the alarm does at Roused
string action = "__ACTION__";
string arg = "__ARG__";
const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
    | System.Reflection.BindingFlags.NonPublic;

System.Type Find(string name)
{
    foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
    {
        var t = asm.GetType(name);
        if (t != null) return t;
    }
    return null;
}
object Get(object o, string prop) => o.GetType().GetProperty(prop, All).GetValue(o);

var doorType = Find("Plunderspell.Castle.CastleDoor");
if (doorType == null) return "no CastleDoor type";
var found = UnityEngine.Object.FindObjectsByType(doorType, UnityEngine.FindObjectsSortMode.None);
var doors = new System.Collections.Generic.List<UnityEngine.Component>();
foreach (var o in found) doors.Add((UnityEngine.Component)o);
doors.Sort((a, b) =>
{
    var pa = a.transform.position; var pb = b.transform.position;
    int c = UnityEngine.Mathf.RoundToInt(pa.x * 10).CompareTo(UnityEngine.Mathf.RoundToInt(pb.x * 10));
    return c != 0 ? c : UnityEngine.Mathf.RoundToInt(pa.z * 10).CompareTo(UnityEngine.Mathf.RoundToInt(pb.z * 10));
});

string Describe(UnityEngine.Component d)
{
    var hinge = (UnityEngine.Transform)doorType.GetField("_hinge", All).GetValue(d);
    return "open " + Get(d, "IsOpen") + " locked " + Get(d, "IsLocked") + " barred " + Get(d, "IsBarred")
        + " hinge " + (hinge != null ? hinge.localEulerAngles.y.ToString("F0") : "?") + " at " + d.transform.position.ToString("F1");
}

if (action == "doors")
{
    int open = 0, locked = 0, barred = 0;
    foreach (var d in doors)
    {
        if ((bool)Get(d, "IsOpen")) open++;
        if ((bool)Get(d, "IsLocked")) locked++;
        if ((bool)Get(d, "IsBarred")) barred++;
    }
    return "doors " + doors.Count + " open " + open + " locked " + locked + " barred " + barred;
}
int n = 0;
int.TryParse(arg, out n);
if (n < 0 || n >= doors.Count) return "no door " + n + " of " + doors.Count;
var door = doors[n];
if (action == "door") return "door " + n + " " + Describe(door);
if (action == "hand")
{
    var handleType = Find("Plunderspell.Loot.CastleDoorHandle");
    var handle = handleType != null ? door.GetComponentInChildren(handleType) : null;
    if (handle == null) return "door " + n + " has no CastleDoorHandle";
    var result = handleType.GetMethod("Interact", All).Invoke(handle, null);
    return "hand on door " + n + " returned " + result;
}
if (action == "porta")
{
    doorType.GetMethod("Open", All, null, System.Type.EmptyTypes, null).Invoke(door, null);
    return "porta on door " + n;
}
if (action == "lockdown")
{
    var lockdownType = Find("Plunderspell.Castle.CastleLockdown");
    var lockdown = UnityEngine.Object.FindFirstObjectByType(lockdownType);
    lockdownType.GetMethod("LockAllDoors", All).Invoke(lockdown, null);
    return "locked down, affected " + Get(lockdown, "DoorsAffected");
}
return "unknown action " + action;

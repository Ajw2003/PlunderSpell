// Play mode only. One side of Tools/Unity/coop_lair_check.sh (#314): runs the same in the Editor (host) and the
// Development build (client), so it reaches the game only through reflection. Rooms are found by scene path,
// because the Market prefab's yard model is also named "MarketYard".
//   where              game state and the local player's position
//   at <path>          how far (x and z only) the local player stands from the scene object at <path>
//   bodies             every player body this side can see, with positions
//   travel <path>      use the RoomTravel at <path> as the local player (the Market door, the Market's way out)
//   pile               the loot pieces within 5 m of the Lair's HaulLanding
//   pad <n>            host: put n raid pieces on the extraction pad
//   shot <path>        save a screenshot (absolute path, forward slashes)
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

string V(UnityEngine.Vector3 v) => v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1");
UnityEngine.Component LocalPlayer() => Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;
object State() => Get(Get(T("Plunderspell.Core.GameServices"), "GameState"), "CurrentState");
UnityEngine.Object[] FindAll(string type) => UnityEngine.Object.FindObjectsByType(T(type), UnityEngine.FindObjectsSortMode.None);

switch (action)
{
    case "where":
    {
        var player = LocalPlayer();
        return "state " + State() + " local " + (player != null ? V(player.transform.position) : "none");
    }
    case "at":
    {
        var player = LocalPlayer();
        var target = UnityEngine.GameObject.Find(arg);
        if (player == null || target == null) return "missing " + (player == null ? "player" : arg);
        var d = player.transform.position - target.transform.position;
        d.y = 0f;
        return "distance " + d.magnitude.ToString("F2") + " from " + arg + " at " + V(target.transform.position);
    }
    case "bodies":
    {
        string s = "";
        foreach (var o in FindAll("StateMachine.PlayerStateMachine"))
            s += V(((UnityEngine.Component)o).transform.position) + "; ";
        return "bodies " + s;
    }
    case "travel":
    {
        var door = UnityEngine.GameObject.Find(arg);
        if (door == null) return "no " + arg;
        var travel = door.GetComponent(T("Plunderspell.Raid.RoomTravel"));
        travel.GetType().GetMethod("Travel").Invoke(travel, new object[] { LocalPlayer() });
        return "travelled through " + arg + " to " + V(LocalPlayer().transform.position);
    }
    case "pile":
    {
        var landing = UnityEngine.GameObject.Find("/LairRoom/HaulLanding").transform;
        // Sorted, so the host's and the client's lines compare equal whatever order each finds them in.
        var names = new System.Collections.Generic.List<string>();
        foreach (var o in FindAll("Plunderspell.Loot.LootValue"))
        {
            var piece = (UnityEngine.Component)o;
            if ((piece.transform.position - landing.position).sqrMagnitude < 25f) names.Add(piece.name);
        }
        names.Sort(System.StringComparer.Ordinal);
        return "pile " + names.Count + " " + string.Join(" ", names);
    }
    case "pad":
    {
        int want = int.Parse(arg);
        var zone = (UnityEngine.Component)FindAll("Plunderspell.Extraction.ExtractionZone")[0];
        var lair = UnityEngine.GameObject.Find("/LairRoom").transform;
        int moved = 0;
        foreach (var o in FindAll("Plunderspell.Loot.LootValue"))
        {
            if (moved == want) break;
            var piece = (UnityEngine.Component)o;
            if ((piece.transform.position - lair.position).sqrMagnitude < 400f || (bool)Get(piece, "IsRuined")) continue;
            var body = piece.GetComponent<UnityEngine.Rigidbody>();
            var at = zone.transform.position + new UnityEngine.Vector3(0.6f * moved - 0.3f, 0.6f, 0.4f);
            if (body != null) { body.isKinematic = false; body.position = at; body.linearVelocity = UnityEngine.Vector3.zero; }
            piece.transform.position = at;
            moved++;
        }
        return "on the pad " + moved;
    }
    case "shot":
        UnityEngine.ScreenCapture.CaptureScreenshot(arg);
        return "screenshot to " + arg;
}
return "unknown action " + action;

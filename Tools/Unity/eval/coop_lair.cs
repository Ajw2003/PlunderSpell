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
//   put <entry>        host: set loot-table entry <entry> down on the Goldsmith's counter (through the pile's spawner, on the server)
//   look               stand the local player 2 m in front of the Goldsmith's counter, facing it
//   line               the Goldsmith counter's subtitle text on this side
//   speak <word>       the local player's word at the Goldsmith counter: Plus, Satis or Vale (what keys 1/2/3 call)
//   money              the Lair's banked gold and debt (read on the host: it is the server)
//   piece              how many loot pieces lie on the Goldsmith counter on this side
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

UnityEngine.Component GoldsmithCounter()
{
    foreach (var o in FindAll("Plunderspell.Raid.SellCounter"))
        if (Get(o, "Vendor").ToString() == "Goldsmith") return (UnityEngine.Component)o;
    throw new System.Exception("no Goldsmith counter");
}

switch (action)
{
    case "put":
    {
        var landing = FindAll("Plunderspell.Raid.HaulLanding")[0];
        var pile = Get(landing, "_pile");
        object table = null;
        foreach (var sp in FindAll("Plunderspell.Raid.LootSpawner"))
            if (sp != pile && Get(sp, "Table") != null) table = Get(sp, "Table");
        var entry = ((System.Collections.IList)Get(table, "Entries"))[int.Parse(arg)];
        var at = GoldsmithCounter().GetComponentInChildren<UnityEngine.BoxCollider>().bounds.center + UnityEngine.Vector3.up * 0.1f;
        var go = (UnityEngine.GameObject)pile.GetType().GetMethod("SpawnLoose").Invoke(pile, new object[] { Get(entry, "Item"), Get(entry, "Prefab"), at });
        return "put " + go.name;
    }
    case "look":
    {
        var counter = GoldsmithCounter().transform;
        var player = LocalPlayer();
        var from = counter.position + (player.transform.position - counter.position).normalized * 2f;
        from.y = player.transform.position.y;
        var look = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(counter.position.x - from.x, 0f, counter.position.z - from.z));
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.isKinematic = true; body.position = from; }
        player.transform.position = from;
        // The camera turns apart from the body (as view.sh does): yaw it at the counter, a little down.
        float yaw = look.eulerAngles.y;
        player.GetType().GetMethod("FaceYaw").Invoke(player, new object[] { yaw });
        UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(8f, yaw, 0f);
        return "looking at the Goldsmith from " + V(from);
    }
    case "line":
        return "line " + ((UnityEngine.TextMesh)Get(GoldsmithCounter(), "_subtitle")).text;
    case "speak":
    {
        var counter = GoldsmithCounter();
        var word = System.Enum.Parse(T("Plunderspell.Market.HaggleWord"), arg);
        counter.GetType().GetMethod("Speak").Invoke(counter, new object[] { word });
        return "spoke " + arg;
    }
    case "money":
    {
        var lair = FindAll("Plunderspell.Lair.LairHubManager")[0];
        return "gold " + Get(lair, "AccumulatedGold") + " debt " + Get(lair, "TotalDebt");
    }
    case "piece":
    {
        var box = GoldsmithCounter().GetComponentInChildren<UnityEngine.BoxCollider>();
        int n = 0;
        foreach (var o in FindAll("Plunderspell.Loot.LootValue"))
            if (box.bounds.Contains(((UnityEngine.Component)o).transform.position)) n++;
        return "pieces " + n;
    }
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

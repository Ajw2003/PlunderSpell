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
//   spawnpile <entry>  host or solo: set loot-table entry <entry> down on the Lair's haul pile, through the pile's spawner (#333)
//   look               stand the local player 2 m in front of the Goldsmith's counter, facing it
//   line               the Goldsmith counter's subtitle text on this side
//   speak <word>       the local player's word at the Goldsmith counter: Plus, Satis or Vale (what keys 1/2/3 call)
//   money              the Lair's banked gold and debt (read on the host: it is the server)
//   piece              how many loot pieces lie on the Goldsmith counter on this side
//   pouch              the coin pouches this side sees (#313): count, then the first one's coins, mass and loot-value presence
//   pouchto <n>        host: move the coin pouch through its rigidbody into strongbox n's lid (n 1 to 4), as if let go there
//   purse <n>          the Lair's purse n (read on the host: it is the server)
//   bank <n>:<coins>   host: bank coins into purse n (1 to 4), as a let-go pouch does
//   ledger             host: the debt, the four purses, paid last collection, seats present and the Collector's line (#313)
//   book               the ledger book's two pages' text on this side (#357)
//   dial               the century dial's Age and plaque text on this side (#358)
//   turndial <era>     host: choose the Age (BronzeAge, HighMedieval, LateMedieval, AgeOfPowder) as the dial's E press does (#358)
//   slot1            what the Lair's saved slot 1 holds (debt, gold, four purses), read without loading it
//   activeslot         the save slot this side plays in and what it has SAVED (debt, gold, four purses, paid), read without loading
//   portalwalk         the local player walks into the portal arch's trigger (#359): a client sees "The host sets out", the host sets out
//   portalline         the opacity of the client's "The host sets out" line at the arch now (0 to 1)
//   pausemenu          open the pause menu on this side, as Esc does (#359)
//   grab pile|pouch    stand the local player 1.8 m from the first pile piece (or the coin pouch), looking at it, and grab it
//                      the way a left-click does (ItemManager.StartDragging, the piece's own point as the grab point) (#333)
//   held               what the local player holds: name, dragging, holders, offset in the player's view frame, speed, position
//   drop               let go of what the local player holds (ItemManager.ForceRelease)
//   lookat <path>      stand the local player 2 m from the scene object at <path>, facing it, a little down, body free
//   findnear <x,z>     the loot pieces and coin pouches within 3 m of that point on this side (names and positions)
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
    case "spawnpile":
    {
        var landing = FindAll("Plunderspell.Raid.HaulLanding")[0];
        var pile = Get(landing, "_pile");
        object table = null;
        foreach (var sp in FindAll("Plunderspell.Raid.LootSpawner"))
            if (sp != pile && Get(sp, "Table") != null) table = Get(sp, "Table");
        var entry = ((System.Collections.IList)Get(table, "Entries"))[int.Parse(arg)];
        var at = ((UnityEngine.Component)landing).transform.position + UnityEngine.Vector3.up * 0.3f;
        var go = (UnityEngine.GameObject)pile.GetType().GetMethod("SpawnLoose").Invoke(pile, new object[] { Get(entry, "Item"), Get(entry, "Prefab"), at });
        return "spawned " + go.name + " on the pile at " + V(at);
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
    case "grab":
    {
        UnityEngine.Component target = null;
        if (arg == "pouch")
        {
            var pouches = FindAll("Plunderspell.Raid.CoinPouch");
            if (pouches.Length == 0) return "no pouch";
            target = (UnityEngine.Component)pouches[0];
        }
        else
        {
            var landing = UnityEngine.GameObject.Find("/LairRoom/HaulLanding").transform;
            foreach (var o in FindAll("Plunderspell.Loot.LootValue"))
            {
                var piece = (UnityEngine.Component)o;
                if ((piece.transform.position - landing.position).sqrMagnitude < 25f && (target == null || string.CompareOrdinal(piece.name, target.name) < 0)) target = piece;
            }
            if (target == null) return "no pile piece";
        }
        var player = LocalPlayer();
        var p = target.transform.position;
        var away = player.transform.position - p; away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = UnityEngine.Vector3.back;
        var from = p + away.normalized * 1.8f; from.y = player.transform.position.y;
        float yaw = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(p.x - from.x, 0f, p.z - from.z)).eulerAngles.y;
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.position = from; }
        player.transform.position = from;
        player.GetType().GetMethod("FaceYaw").Invoke(player, new object[] { yaw });
        var cam = UnityEngine.Camera.main.transform;
        float pitch = UnityEngine.Mathf.Atan2(cam.position.y - p.y, 1.8f) * UnityEngine.Mathf.Rad2Deg;
        cam.localRotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0f);
        var items = Get(T("ItemManager"), "Instance");
        items.GetType().GetMethod("StartDragging", All).Invoke(items, new object[] { target.GetComponent(T("Item")), p });
        return "grabbed " + target.name + " at " + V(p) + " from " + V(from);
    }
    case "held":
    {
        var item = Get(Get(T("ItemManager"), "Instance"), "CarriedItem") as UnityEngine.Component;
        if (item == null) return "held none";
        var player = LocalPlayer();
        var cam = UnityEngine.Camera.main.transform;
        var rel = UnityEngine.Quaternion.Inverse(UnityEngine.Quaternion.Euler(0f, cam.eulerAngles.y, 0f)) * (item.transform.position - player.transform.position);
        return "held " + item.name + " dragging " + Get(item, "IsDragging") + " holders " + Get(item, "HolderCount") + " rel " + V(rel)
            + " speed " + item.GetComponent<UnityEngine.Rigidbody>().linearVelocity.magnitude.ToString("F2") + " at " + V(item.transform.position) + " body " + V(player.transform.position);
    }
    case "drop":
    {
        var items = Get(T("ItemManager"), "Instance");
        items.GetType().GetMethod("ForceRelease").Invoke(items, null);
        return "dropped";
    }
    case "lookat":
    {
        var at = UnityEngine.GameObject.Find(arg);
        if (at == null) return "no " + arg;
        var player = LocalPlayer();
        var away = player.transform.position - at.transform.position; away.y = 0f;
        var from = at.transform.position + away.normalized * 2f; from.y = player.transform.position.y;
        float yaw = UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(at.transform.position.x - from.x, 0f, at.transform.position.z - from.z)).eulerAngles.y;
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.position = from; }
        player.transform.position = from;
        player.GetType().GetMethod("FaceYaw").Invoke(player, new object[] { yaw });
        UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(8f, yaw, 0f);
        return "standing at " + V(from) + " looking at " + arg;
    }
    case "findnear":
    {
        var parts = arg.Split(',');
        var centre = new UnityEngine.Vector3(float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture), 0f, float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture));
        string s = "";
        foreach (var type in new[] { "Plunderspell.Loot.LootValue", "Plunderspell.Raid.CoinPouch" })
            foreach (var o in FindAll(type))
            {
                var piece = (UnityEngine.Component)o;
                var d = piece.transform.position - centre; d.y = 0f;
                if (d.magnitude < 3f) s += piece.name + " " + V(piece.transform.position) + "; ";
            }
        return "near " + s;
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
    case "pouch":
    {
        var pouches = FindAll("Plunderspell.Raid.CoinPouch");
        if (pouches.Length == 0) return "pouches 0";
        var p = (UnityEngine.Component)pouches[0];
        return "pouches " + pouches.Length + " coins " + Get(p, "Coins") + " mass " + p.GetComponent<UnityEngine.Rigidbody>().mass.ToString("F2")
            + " lootvalue " + (p.GetComponent(T("Plunderspell.Loot.LootValue")) != null);
    }
    case "pouchto":
    {
        int seat = int.Parse(arg) - 1;
        var p = (UnityEngine.Component)FindAll("Plunderspell.Raid.CoinPouch")[0];
        foreach (var o in FindAll("Plunderspell.Raid.LairStrongbox"))
        {
            if ((int)Get(o, "Seat") != seat) continue;
            var body = p.GetComponent<UnityEngine.Rigidbody>();
            var at = ((UnityEngine.Component)o).GetComponent<UnityEngine.BoxCollider>().bounds.center;
            body.linearVelocity = UnityEngine.Vector3.zero; body.position = at; p.transform.position = at;
            return "pouch set into strongbox " + arg + " at " + V(at);
        }
        return "no strongbox " + arg;
    }
    case "purse":
    {
        var lair = FindAll("Plunderspell.Lair.LairHubManager")[0];
        return "purse " + lair.GetType().GetMethod("Purse").Invoke(lair, new object[] { int.Parse(arg) - 1 });
    }
    case "bank":
    {
        // host: bank <seat 1 to 4>:<coins> into that purse, as a let-go pouch would (LairHubManager.BankPouch)
        var lair = FindAll("Plunderspell.Lair.LairHubManager")[0];
        var parts = arg.Split(':');
        lair.GetType().GetMethod("BankPouch").Invoke(lair, new object[] { int.Parse(parts[0]) - 1, int.Parse(parts[1]) });
        return "banked " + parts[1] + " into purse " + parts[0];
    }
    case "ledger":
    {
        // host: the debt, the four purses, what each paid at the last collection, the seats present, the Collector's line
        var lair = FindAll("Plunderspell.Lair.LairHubManager")[0];
        var t = lair.GetType();
        string s = "debt " + Get(lair, "TotalDebt") + " purses";
        for (int seat = 0; seat < 4; seat++) s += " " + t.GetMethod("Purse").Invoke(lair, new object[] { seat });
        s += " paid";
        for (int seat = 0; seat < 4; seat++) s += " " + t.GetMethod("PaidLast").Invoke(lair, new object[] { seat });
        s += " present";
        for (int seat = 0; seat < 4; seat++) s += " " + ((bool)t.GetMethod("IsPresent").Invoke(lair, new object[] { seat }) ? 1 : 0);
        return s + " line " + Get(lair, "CollectorLine");
    }
    case "book":
    {
        // what the ledger book on the Lair table says on this side (#357), newlines as "/", so host and client lines compare equal
        var book = FindAll("Plunderspell.Raid.LairLedgerBook")[0];
        return "book left [" + ((string)Get(book, "LeftText")).Replace("\n", "/") + "] right [" + ((string)Get(book, "RightText")).Replace("\n", "/") + "]";
    }
    case "dial":
    {
        // the century dial's Age and plaque on this side (#358), newlines as "/"
        var dial = FindAll("Plunderspell.Raid.LairCenturyDial")[0];
        return "dial " + Get(dial, "ShownEra") + " [" + ((string)Get(dial, "PlaqueText")).Replace("\n", "/") + "]";
    }
    case "turndial":
    {
        // host: choose an Age by name through LairHubManager.SelectEra, the call the dial's E press makes (#358)
        var hub = T("Plunderspell.Lair.LairHubManager");
        var chosen = FindAll("Plunderspell.Lair.LairHubManager")[0];
        hub.GetMethod("SelectEra").Invoke(chosen, new object[] { System.Enum.Parse(T("Plunderspell.Inventory.HistoricalEra"), arg) });
        return "chose " + arg;
    }
    case "slot1":
    {
        var hub = T("Plunderspell.Lair.LairHubManager");
        var state = hub.GetMethod("Peek").Invoke(null, new object[] { 1 });
        string s = "slot1 debt " + Get(state, "TotalDebt") + " gold " + Get(state, "AccumulatedGold") + " purses";
        for (int seat = 0; seat < 4; seat++) s += " " + hub.GetMethod("PeekPurse").Invoke(null, new object[] { 1, seat });
        return s;
    }
    case "activeslot":
    {
        var hub = T("Plunderspell.Lair.LairHubManager");
        int slot = (int)Get(T("Plunderspell.Lair.SaveSlots"), "Active");
        var state = hub.GetMethod("Peek").Invoke(null, new object[] { slot });
        string s = "slot " + slot + " saved debt " + Get(state, "TotalDebt") + " gold " + Get(state, "AccumulatedGold") + " purses";
        for (int seat = 0; seat < 4; seat++) s += " " + hub.GetMethod("PeekPurse").Invoke(null, new object[] { slot, seat });
        s += " paid";
        for (int seat = 0; seat < 4; seat++) s += " " + hub.GetMethod("PeekPaidLast").Invoke(null, new object[] { slot, seat });
        return s;
    }
    case "portalwalk":
    {
        // the local player walks into the portal arch's trigger (#359): stands 1.2 m short of it, facing the arch, then steps into it
        var trigger = (UnityEngine.Component)FindAll("Plunderspell.Raid.LairPortalTrigger")[0];
        var centre = trigger.GetComponent<UnityEngine.BoxCollider>().bounds.center;
        var player = LocalPlayer();
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        var short_ = new UnityEngine.Vector3(centre.x - 2.4f, player.transform.position.y, centre.z);
        if (body != null) { body.linearVelocity = UnityEngine.Vector3.zero; body.position = short_; }
        player.transform.position = short_;
        player.GetType().GetMethod("FaceYaw").Invoke(player, new object[] { 90f });
        UnityEngine.Camera.main.transform.localRotation = UnityEngine.Quaternion.Euler(0f, 90f, 0f);
        var into = new UnityEngine.Vector3(centre.x, short_.y, centre.z);
        if (body != null) body.position = into;
        player.transform.position = into;
        return "walked into the portal at " + V(into);
    }
    case "portalline":
    {
        var line = Get(FindAll("Plunderspell.Raid.LairPortalTrigger")[0], "HostLine");
        return "portal line alpha " + ((float)Get(line, "Alpha")).ToString("F2") + " peak " + ((float)Get(line, "PeakAlpha")).ToString("F2");
    }
    case "pausemenu":
    {
        // show the pause menu from where the player stands (Esc, as GameFlowInput does)
        var states = Get(T("Plunderspell.Core.GameServices"), "GameState");
        states.GetType().GetMethod("ChangeState").Invoke(states, new object[] { System.Enum.Parse(T("Plunderspell.Core.GameState"), "Paused") });
        return "state " + State() + " resumes to " + Get(states, "PausedFrom");
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

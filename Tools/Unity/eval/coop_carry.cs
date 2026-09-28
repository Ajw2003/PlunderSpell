// Play mode only. One side of Tools/Unity/coop_carry_check.sh: runs the same way in the Editor (the
// host) and in a Development build (the client), so it reaches the game only through reflection,
// which is all the build's eval can do. coop_carry_check.sh replaces the two placeholders below.
//   host_udp          host a local-network session and go to the Lair
//   state             one line: game state, connection, local player position, what is held
//   stage <kind>:<n>  host: move the n-th spawned piece of that weight ("light" up to 3 kg, "heavy"
//                     10-16 kg) in front of the host's player and print its network id. A fresh n per
//                     scenario, so nothing one scenario did to a piece carries into the next.
//   beside <x,y,z>    put the local player at that point, e.g. next to the host
//   aim <id>          turn the local view onto the piece
//   grab <id>         grab the piece at the point the crosshair is on, as a left-click does
//   release           let go
//   throw             throw the dragged item, as a right-click does
//   depth <metres>    set how far along the aim ray the held point sits (pulls it in or out)
//   turn <degrees>    turn the local view about world up
//   pitch <degrees>   look at this pitch (degrees, positive down), like level but to an angle
//   level             look level, so a held piece is lifted to eye height
//   read <id>         the piece's position, whether this side holds it, where it aims it, and
//                     where its held point is
//   park <id>         host: move a finished scenario's piece out of the way
//   shot <path>       save a screenshot (absolute path, forward slashes)
//   quit              close this game (the client build)
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
    for (var t = target.GetType(); t != null; t = t.BaseType)
    {
        var field = t.GetField(name, All);
        if (field != null) { field.SetValue(target, value); return; }
    }
    throw new System.Exception("no field " + name + " on " + target.GetType().Name);
}

object Call(object target, string name, params object[] args)
{
    var type = target as System.Type ?? target.GetType();
    object instance = target is System.Type ? null : target;
    for (var t = type; t != null; t = t.BaseType)
    {
        foreach (var method in t.GetMethods(All))
        {
            if (method.Name == name && method.GetParameters().Length == args.Length)
                return method.Invoke(instance, args);
        }
    }
    throw new System.Exception("no method " + name + "/" + args.Length + " on " + type.Name);
}

UnityEngine.Object[] FindAll(string typeName) =>
    UnityEngine.Object.FindObjectsByType(T(typeName), UnityEngine.FindObjectsInactive.Exclude, UnityEngine.FindObjectsSortMode.None);

UnityEngine.Component LocalPlayer() => Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;

UnityEngine.Component Piece(string id)
{
    foreach (var found in FindAll("Item"))
    {
        var item = (UnityEngine.Component)found;
        var identity = item.GetComponentInParent(T("PurrNet.NetworkIdentity"));
        if (identity != null && Get(identity, "objectId").ToString() == id) return item;
    }
    throw new System.Exception("no piece with network id " + id);
}

UnityEngine.Component Items() => Get(T("ItemManager"), "Instance") as UnityEngine.Component;

string V(UnityEngine.Vector3 v) => v.x.ToString("F3") + "," + v.y.ToString("F3") + "," + v.z.ToString("F3");

UnityEngine.Vector3 ParseV(string s)
{
    var parts = s.Split(',');
    return new UnityEngine.Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
}

var gameStates = T("Plunderspell.Core.GameServices");
switch (action)
{
    case "host_udp":
    {
        var session = FindAll("Plunderspell.Net.CoopSession")[0];
        Call(session, "StartHost", Get(session, "_udpTransport"), "Hosting on the local network (carry check).");
        var manager = Get(gameStates, "GameState");
        Call(manager, "ChangeState", System.Enum.Parse(T("Plunderspell.Core.GameState"), "Lair"));
        return "hosting";
    }
    case "state":
    {
        var player = LocalPlayer();
        var held = Items() != null ? Get(Items(), "CarriedItem") as UnityEngine.Component : null;
        int players = FindAll("StateMachine.PlayerStateMachine").Length;
        return "state " + Get(Get(gameStates, "GameState"), "CurrentState") + " players " + players
            + " local " + (player != null ? V(player.transform.position) : "none")
            + " holding " + (held != null ? held.name : "nothing");
    }
    case "stage":
    {
        bool heavy = arg.StartsWith("heavy");
        int skip = int.Parse(arg.Substring(arg.IndexOf(':') + 1));
        var player = LocalPlayer();
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        // Face the most open way: each scenario turns a view 25 degrees and pulls a piece ~2 m out,
        // and a wall or furniture in that path pins the piece and reads as a carry fault. A way
        // counts as open as far as it and 30 degrees either side are clear at chest height.
        var chest = view.position - UnityEngine.Vector3.up * 0.3f;
        float bestYaw = 0f, bestClear = -1f;
        for (int step = 0; step < 16; step++)
        {
            float yawTry = step * 22.5f;
            float clear = float.MaxValue;
            foreach (float spread in new[] { -30f, 0f, 30f })
            {
                var way = UnityEngine.Quaternion.Euler(0f, yawTry + spread, 0f) * UnityEngine.Vector3.forward;
                float reach = UnityEngine.Physics.Raycast(chest, way, out var wall, 6f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)
                    ? wall.distance : 6f;
                clear = UnityEngine.Mathf.Min(clear, reach);
            }
            if (clear > bestClear) { bestClear = clear; bestYaw = yawTry; }
        }
        Set(player, "_yaw", bestYaw);
        Set(player, "_xRotation", 0f);
        view.localRotation = UnityEngine.Quaternion.Euler(0f, bestYaw, 0f);
        var forward = UnityEngine.Quaternion.Euler(0f, bestYaw, 0f) * UnityEngine.Vector3.forward;
        var right = UnityEngine.Quaternion.Euler(0f, bestYaw, 0f) * UnityEngine.Vector3.right;
        foreach (var found in FindAll("Plunderspell.Loot.LootPickup"))
        {
            var pickup = (UnityEngine.Component)found;
            var body = pickup.GetComponent<UnityEngine.Rigidbody>();
            if (body == null || !(bool)Get(pickup, "isSpawned") || (bool)Get(pickup, "IsBroken")) continue;
            if (heavy ? body.mass <= 10f || body.mass > 16f : body.mass > 3f) continue;
            // Weapons are held in the hand, one player at a time; the carry check is about pieces.
            if (pickup.GetComponent(T("Item")) == null || pickup.GetComponent(T("RangedWeapon")) != null
                || pickup.GetComponent(T("MeleeWeapon")) != null) continue;
            if (skip-- > 0) continue;
            var at = player.transform.position + forward * 2.2f + UnityEngine.Vector3.up * 1.0f;
            body.isKinematic = false;
            body.position = at;
            pickup.transform.position = at;
            body.linearVelocity = UnityEngine.Vector3.zero;
            body.angularVelocity = UnityEngine.Vector3.zero;
            return Get(pickup, "objectId") + " " + pickup.name + " " + body.mass.ToString("F1") + "kg at " + V(at)
                + " clear " + bestClear.ToString("F1") + "m host " + V(player.transform.position) + " right " + V(right);
        }
        return "no spawned " + arg + " piece";
    }
    case "beside":
    {
        var player = LocalPlayer();
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        var at = ParseV(arg);
        body.position = at;
        player.transform.position = at;
        body.linearVelocity = UnityEngine.Vector3.zero;
        return "placed at " + V(at);
    }
    case "aim":
    {
        var player = LocalPlayer();
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        var to = Piece(arg).GetComponent<UnityEngine.Rigidbody>().worldCenterOfMass - view.position;
        float yaw = UnityEngine.Mathf.Atan2(to.x, to.z) * UnityEngine.Mathf.Rad2Deg;
        float pitch = -UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
        Set(player, "_yaw", yaw);
        Set(player, "_xRotation", pitch);
        view.localRotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0f);
        return "aimed yaw " + yaw.ToString("F1") + " pitch " + pitch.ToString("F1") + " distance " + to.magnitude.ToString("F2");
    }
    case "grab":
    {
        var item = Piece(arg);
        var player = LocalPlayer();
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        var grabPoint = item.GetComponent<UnityEngine.Rigidbody>().worldCenterOfMass;
        if (UnityEngine.Physics.Raycast(view.position, view.forward, out var hit, 10f)
            && hit.collider.GetComponentInParent(T("Item")) == item)
            grabPoint = hit.point;
        Call(Items(), "StartDragging", item, grabPoint);
        return "grab asked at " + V(grabPoint);
    }
    case "release":
        Call(Items(), "ForceRelease");
        return "released";
    case "throw":
        Call(Items(), "ThrowDraggedItem");
        return "thrown";
    case "depth":
        Set(Items(), "_currentDragDepth", float.Parse(arg));
        return "depth " + arg;
    case "turn":
    {
        var player = LocalPlayer();
        float yaw = (float)Get(player, "_yaw") + float.Parse(arg);
        Set(player, "_yaw", yaw);
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        view.localRotation = UnityEngine.Quaternion.Euler((float)Get(player, "_xRotation"), yaw, 0f);
        return "turned to yaw " + yaw.ToString("F1");
    }
    case "level":
    {
        var player = LocalPlayer();
        Set(player, "_xRotation", 0f);
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        view.localRotation = UnityEngine.Quaternion.Euler(0f, (float)Get(player, "_yaw"), 0f);
        return "level";
    }
    case "pitch":
    {
        var player = LocalPlayer();
        float pitch = float.Parse(arg);
        Set(player, "_xRotation", pitch);
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        view.localRotation = UnityEngine.Quaternion.Euler(pitch, (float)Get(player, "_yaw"), 0f);
        return "pitched to " + pitch.ToString("F1");
    }
    case "read":
    {
        var item = Piece(arg);
        var held = Items() != null ? Get(Items(), "CarriedItem") as UnityEngine.Component : null;
        var aim = held == item ? V((UnityEngine.Vector3)Get(item, "TargetPosition")) : "none";
        var grip = held == item ? V((UnityEngine.Vector3)Get(item, "GripWorldPosition")) : "none";
        var body = item.GetComponent<UnityEngine.Rigidbody>();
        var identity = item.GetComponentInParent(T("PurrNet.NetworkIdentity"));
        return "pos " + V(item.transform.position) + " held " + (held == item) + " aim " + aim + " grip " + grip
            + " | kinematic " + body.isKinematic + " portalFrozen " + Get(item, "IsFrozenByPortal")
            + " inPortal " + Get(item, "_inPortal") + " controls " + ((System.Delegate)Get(T("Item"), "CanDriveHere")).DynamicInvoke(item)
            + " holders " + Get(item, "HolderCount") + " owner " + Get(identity, "owner");
    }
    case "park":
    {
        // A finished scenario's piece, out of the way: left where it dropped, the next scenario's
        // piece caught on it and read as a carry fault.
        var body = Piece(arg).GetComponent<UnityEngine.Rigidbody>();
        body.isKinematic = true;
        body.position += UnityEngine.Vector3.down * 50f;
        body.transform.position = body.position;
        return "parked " + arg;
    }
    case "shot":
        UnityEngine.ScreenCapture.CaptureScreenshot(arg);
        return "screenshot to " + arg;
    case "quit":
        UnityEngine.Application.Quit();
        return "quitting";
}
return "unknown action " + action;

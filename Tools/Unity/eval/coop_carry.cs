// Play mode only. One side of Tools/Unity/coop_carry_check.sh: runs the same way in the Editor (the
// host) and in a Development build (the client), so it reaches the game only through reflection,
// which is all the build's eval can do. coop_carry_check.sh replaces the two placeholders below.
//   host_udp          host a local-network session and go to the Lair room
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
//   face_open         turn the local view to the most open way
//   relocate          host: move to the most open walkable spot within 25 m (once per session)
//   pitch <degrees>   look at this pitch (degrees, positive down), like level but to an angle
//   level             look level, so a held piece is lifted to eye height
//   read <id>         the piece's position, whether this side holds it, where it aims it, where
//                     its held point is, its load and whether it is too heavy to lift
//   park <id>         host: move a finished scenario's piece out of the way
//   trace_start <id>  record the piece every physics step (replaces any running trace)
//   trace_stop <path> stop recording and write the CSV there (absolute path, forward slashes)
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

// Takes the running trace's step out of the player loop and returns its rows (null if none runs).
System.Text.StringBuilder StopTrace()
{
    if (!(System.AppDomain.CurrentDomain.GetData("coopTrace") is object[] running)) return null;
    System.AppDomain.CurrentDomain.SetData("coopTrace", null);
    var step = (System.Delegate)running[0];
    var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
    for (int i = 0; i < loop.subSystemList.Length; i++)
    {
        var steps = loop.subSystemList[i].subSystemList;
        if (steps == null) continue;
        loop.subSystemList[i].subSystemList = System.Array.FindAll(steps, s => !ReferenceEquals(s.updateDelegate, step));
    }
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
    return (System.Text.StringBuilder)running[1];
}

// The most open horizontal way from this view, in degrees of yaw, and how far it is clear.
float OpenYaw(UnityEngine.Vector3 eye, out float bestClear)
{
    // Face the most open way: each scenario turns a view 25 degrees and pulls a piece ~2 m out,
    // and a wall or furniture in that path pins the piece and reads as a carry fault. A way
    // counts as open as far as it and 30 degrees either side are clear at chest height and at the
    // height held pieces ride (eye height and a little above), where wall lamps and shelves are.
    var chest = eye - UnityEngine.Vector3.up * 0.3f;
    var heights = new[] { -0.3f, 0f, 0.4f };
    // The floor this spot stands on: a way whose floor steps up or drops (a dais edge, a stair) is
    // blocked there, since a staged piece falls off the edge and is pinned under its lip.
    float floorY = UnityEngine.Physics.Raycast(eye, UnityEngine.Vector3.down, out var under, 4f, ~0,
        UnityEngine.QueryTriggerInteraction.Ignore) ? under.point.y : eye.y - 1.7f;
    // Not towards the extraction portal either: it is open space, but it holds up and freezes
    // pieces in it, which reads as a carry fault too.
    var portals = new System.Collections.Generic.List<UnityEngine.Bounds>();
    foreach (var zone in FindAll("Plunderspell.Extraction.ExtractionZone"))
    {
        var bounds = ((UnityEngine.Component)zone).GetComponent<UnityEngine.Collider>().bounds;
        bounds.Expand(2f);
        portals.Add(bounds);
    }
    float bestYaw = 0f;
    bestClear = -1f;
    for (int step = 0; step < 16; step++)
    {
        float yawTry = step * 22.5f;
        float clear = float.MaxValue;
        foreach (float spread in new[] { -30f, 0f, 30f })
        {
            var way = UnityEngine.Quaternion.Euler(0f, yawTry + spread, 0f) * UnityEngine.Vector3.forward;
            float reach = 6f;
            // From this player and from where the second player stands (1.2 m to the right, see
            // coop_carry_check.sh's stage): they aim across at the piece, and a wall only this spot
            // missed put their aim point behind it.
            var sideStep = UnityEngine.Quaternion.Euler(0f, yawTry, 0f) * UnityEngine.Vector3.right * 1.2f;
            if (!UnityEngine.Physics.Raycast(eye + sideStep, UnityEngine.Vector3.down, out var sideFloor, 4f, ~0,
                    UnityEngine.QueryTriggerInteraction.Ignore)
                || UnityEngine.Mathf.Abs(sideFloor.point.y - floorY) > 0.2f)
                reach = 0f;
            foreach (var origin in new[] { eye, eye + sideStep })
                foreach (float height in heights)
                    if (UnityEngine.Physics.SphereCast(origin + UnityEngine.Vector3.up * height, 0.2f, way, out var wall, 6f,
                            ~0, UnityEngine.QueryTriggerInteraction.Ignore)
                        && wall.collider.GetComponentInParent(T("Item")) == null)
                        reach = UnityEngine.Mathf.Min(reach, wall.distance);
            for (float along = 0.5f; along < reach; along += 0.5f)
            {
                var above = eye + way * along;
                if (!UnityEngine.Physics.Raycast(above, UnityEngine.Vector3.down, out var floor, 4f, ~0,
                        UnityEngine.QueryTriggerInteraction.Ignore)
                    || UnityEngine.Mathf.Abs(floor.point.y - floorY) > 0.2f)
                {
                    reach = along;
                    break;
                }
            }
            clear = UnityEngine.Mathf.Min(clear, reach);
            for (float along = 0f; along <= reach; along += 0.5f)
                foreach (var portal in portals)
                    if (portal.Contains(chest + way * along)) clear = 0f;
        }
        if (clear > bestClear) { bestClear = clear; bestYaw = yawTry; }
    }
    return bestYaw;
}

var gameStates = T("Plunderspell.Core.GameServices");
switch (action)
{
    case "host_udp":
    {
        var session = FindAll("Plunderspell.Net.CoopSession")[0];
        Call(session, "StartHost", Get(session, "_udpTransport"), "Hosting on the local network (carry check).");
        var manager = Get(gameStates, "GameState");
        Call(manager, "ChangeState", System.Enum.Parse(T("Plunderspell.Core.GameState"), "LairRoom"));   // the room, as CoopSession's own host paths go (#309)
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
        float bestYaw = OpenYaw(view.position, out float bestClear);
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
            // A light piece bigger than a metre (a banner pole, a long rod) jams
            // against the room's walls when held out at eye height, and reads as a carry fault.
            if (!heavy)
            {
                // All its colliders together: a piece can be built from several parts each under a metre.
                var colliders = pickup.GetComponentsInChildren<UnityEngine.Collider>();
                var whole = colliders.Length > 0 ? colliders[0].bounds : new UnityEngine.Bounds(pickup.transform.position, UnityEngine.Vector3.zero);
                foreach (var part in colliders)
                    whole.Encapsulate(part.bounds);
                var size = whole.size;
                if (UnityEngine.Mathf.Max(size.x, UnityEngine.Mathf.Max(size.y, size.z)) > 1f) continue;
            }
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
    case "relocate":
    {
        // Host, once per session: move to the most open walkable spot within 25 m, so every
        // scenario has room to lift, turn and throw. The spawn room of a castle can be too tight
        // for that, and a piece held against its wall reads as a carry fault.
        var player = LocalPlayer();
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        var eyeOffset = view.position - player.transform.position;
        float standHeight = UnityEngine.Physics.Raycast(player.transform.position, UnityEngine.Vector3.down, out var ground, 5f,
            ~0, UnityEngine.QueryTriggerInteraction.Ignore) ? ground.distance : 1.25f;
        var start = player.transform.position;
        var best = start;
        OpenYaw(view.position, out float bestClear);
        float startClear = bestClear;
        // Anywhere walkable nearby, at any height: the scan itself refuses a way that steps
        // up or drops, so a spot on another floor is as good as one on the spawn's.
        // Triangle centres, not vertices: the vertices sit in the corners, against the walls.
        var mesh = UnityEngine.AI.NavMesh.CalculateTriangulation();
        int triangles = mesh.indices.Length / 3;
        // Within 25 m of the spawn, and at most 150 tried: a spot 35 m away left the client not
        // seeing the staged pieces, and every spot tried costs a full scan (an eval must finish in 5 s).
        var near = new System.Collections.Generic.List<UnityEngine.Vector3>();
        for (int t = 0; t < triangles; t++)
        {
            var centre = (mesh.vertices[mesh.indices[3 * t]] + mesh.vertices[mesh.indices[3 * t + 1]]
                + mesh.vertices[mesh.indices[3 * t + 2]]) / 3f;
            if ((centre - start).sqrMagnitude < 25f * 25f) near.Add(centre);
        }
        int stride = UnityEngine.Mathf.Max(1, near.Count / 150);
        for (int t = 0; t < near.Count; t += stride)
        {
            var centre = near[t];
            var stand = centre + UnityEngine.Vector3.up * standHeight;
            OpenYaw(stand + eyeOffset, out float clear);
            if (clear > bestClear + 0.25f) { bestClear = clear; best = stand; }
        }
        body.position = best;
        player.transform.position = best;
        body.linearVelocity = UnityEngine.Vector3.zero;
        return "relocated from " + V(start) + " (clear " + startClear.ToString("F1") + "m) to " + V(best) + " (clear "
            + bestClear.ToString("F1") + "m)";
    }
    case "face_open":
    {
        // Turn the view (and so a held piece) to the most open way, e.g. before a throw that must
        // not hit a wall at once.
        var player = LocalPlayer();
        var view = (UnityEngine.Transform)Get(player, "CameraTransform");
        float yaw = OpenYaw(view.position, out float clear);
        Set(player, "_yaw", yaw);
        view.localRotation = UnityEngine.Quaternion.Euler((float)Get(player, "_xRotation"), yaw, 0f);
        return "facing yaw " + yaw.ToString("F1") + " clear " + clear.ToString("F1") + "m";
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
        // Onto the piece where it is now, as a player's crosshair must be to grab it: aimed where it
        // was staged, after another holder lifted it, the new hold's target starts metres away.
        var to = grabPoint - view.position;
        float grabYaw = UnityEngine.Mathf.Atan2(to.x, to.z) * UnityEngine.Mathf.Rad2Deg;
        float grabPitch = -UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
        Set(player, "_yaw", grabYaw);
        Set(player, "_xRotation", grabPitch);
        view.localRotation = UnityEngine.Quaternion.Euler(grabPitch, grabYaw, 0f);
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
            + " load " + ((float)Get(item, "Load")).ToString("F2") + " heavy " + Get(item, "IsTooHeavyToLift")
            + " | kinematic " + body.isKinematic + " portalFrozen " + Get(item, "IsFrozenByPortal")
            + " inPortal " + Get(item, "_inPortal") + " controls " + ((System.Delegate)Get(T("Item"), "CanDriveHere")).DynamicInvoke(item)
            + " holders " + Get(item, "HolderCount") + " owner " + Get(identity, "owner")
            + " broken " + Get(item.GetComponent(T("Plunderspell.Loot.LootPickup")), "IsBroken")
            + " snapped " + Get(item, "LocalBeamSnapped");
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
    case "trace_start":
    {
        // Every physics step from now until trace_stop, one CSV row: the piece's motion and who
        // holds it on this side. A step hook in the player loop, not a component, because this
        // code is compiled fresh on each call and the build cannot add a component from it.
        StopTrace();
        var item = Piece(arg);
        var body = item.GetComponent<UnityEngine.Rigidbody>();
        var items = Items();
        var carried = items.GetType().GetProperty("CarriedItem", All);
        var holderCount = item.GetType().GetProperty("HolderCount", All);
        var target = item.GetType().GetProperty("TargetPosition", All);
        var totalGrip = item.GetType().GetProperty("TotalGrip", All);
        var tooHeavy = item.GetType().GetProperty("IsTooHeavyToLift", All);
        var canDrive = (System.Delegate)Get(T("Item"), "CanDriveHere");
        var rows = new System.Text.StringBuilder(
            "t,x,y,z,vx,vy,vz,speed,spin,rx,ry,rz,held,holders,controls,kinematic,grip,heavy,aimx,aimy,aimz\n");
        float startedAt = UnityEngine.Time.fixedTime;
        int count = 0;
        UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction step = () =>
        {
            if (item == null || body == null || count >= 6000) return;
            count++;
            var p = body.position; var v = body.linearVelocity; var e = body.rotation.eulerAngles;
            bool held = (UnityEngine.Object)carried.GetValue(items) == item;
            var aim = held ? V((UnityEngine.Vector3)target.GetValue(item)) : ",,";
            rows.Append((UnityEngine.Time.fixedTime - startedAt).ToString("F3")).Append(',').Append(V(p)).Append(',')
                .Append(V(v)).Append(',').Append(v.magnitude.ToString("F3")).Append(',')
                .Append(body.angularVelocity.magnitude.ToString("F3")).Append(',').Append(V(e)).Append(',')
                .Append(held ? 1 : 0).Append(',').Append(holderCount.GetValue(item)).Append(',')
                .Append((bool)canDrive.DynamicInvoke(item) ? 1 : 0).Append(',').Append(body.isKinematic ? 1 : 0).Append(',')
                .Append(((float)totalGrip.GetValue(item)).ToString("F0")).Append(',').Append((bool)tooHeavy.GetValue(item) ? 1 : 0).Append(',')
                .Append(aim).Append('\n');
        };
        var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
        for (int i = 0; i < loop.subSystemList.Length; i++)
        {
            if (loop.subSystemList[i].type != typeof(UnityEngine.PlayerLoop.FixedUpdate)) continue;
            var steps = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(loop.subSystemList[i].subSystemList);
            // Last in the fixed step, so each row is the state physics just produced.
            steps.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(System.Text.StringBuilder), updateDelegate = step });
            loop.subSystemList[i].subSystemList = steps.ToArray();
        }
        UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
        System.AppDomain.CurrentDomain.SetData("coopTrace", new object[] { step, rows });
        return "tracing " + arg;
    }
    case "trace_stop":
    {
        var rows = StopTrace();
        if (rows == null) return "no trace running";
        System.IO.File.WriteAllText(arg, rows.ToString());
        return "trace to " + arg;
    }
    case "shot":
        UnityEngine.ScreenCapture.CaptureScreenshot(arg);
        return "screenshot to " + arg;
    case "quit":
        UnityEngine.Application.Quit();
        return "quitting";
}
return "unknown action " + action;

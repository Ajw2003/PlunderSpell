// Play mode only. One side of Tools/Unity/ophan_check.sh (#418): runs the same way in the Editor (the host) and in a
// Development build (the client), so it reaches the game only through reflection. The script replaces the placeholders.
//   huntoff          switch automatic hunts off on this machine (OphanOfHours.HuntOverride), so the check decides when
//   noguards         host: remove every guard, so only the Ophan hurts anyone
//   huntauto         undo huntoff (the static outlives Play mode when domain reload is off)
//   arrive           host: the Ophan appears now
//   read             the Ophan's state, target and whether it is beaming as this machine shows it; scorched rooms;
//                    every player's health and dead flag; what the local player holds
//   face             turn the local view to look at the Ophan's eye
//   place open|cover put the local player on a castle floor with a clear line to the Ophan's eye (open) or with
//                    castle stone between (cover); prints where
//   grabartifact     host: bring a spawned artifact piece in front of the local player and grab it
//   grabany          bring the most valuable spawned non-artifact piece in front of the local player and grab it
//   release          let go of whatever is held
//   roomshot <path>  save a view of the first scorched room from a camera above it (absolute path, forward slashes)
//   liftshot <path>  save a view up the beam past the piece the Ophan is taking (absolute path, forward slashes)
//   slow <scale>     set Time.timeScale on this machine (1 restores)
//   shot <path>      save a screenshot of this machine's view
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

UnityEngine.Component LocalPlayer() => Get(T("StateMachine.PlayerStateMachine"), "Local") as UnityEngine.Component;
UnityEngine.Component Items() => Get(T("ItemManager"), "Instance") as UnityEngine.Component;
UnityEngine.Component Ophan() => UnityEngine.Object.FindFirstObjectByType(T("Plunderspell.Raid.OphanOfHours")) as UnityEngine.Component;
string V(UnityEngine.Vector3 v) => v.x.ToString("F1") + "," + v.y.ToString("F1") + "," + v.z.ToString("F1");

UnityEngine.Vector3 EyeHere()
{
    // Remembered between calls (each eval is its own script), so a placing between two arrivals can still aim.
    var look = Get(Ophan(), "Look") as UnityEngine.Component;
    if (look == null)
    {
        var last = System.AppDomain.CurrentDomain.GetData("ophanCheckEye");
        if (last == null) throw new System.Exception("the Ophan is not showing on this machine");
        return (UnityEngine.Vector3)last;
    }
    var eye = (UnityEngine.Vector3)Get(look, "EyePoint");
    System.AppDomain.CurrentDomain.SetData("ophanCheckEye", eye);
    return eye;
}

void Look(UnityEngine.Component player, UnityEngine.Vector3 at)
{
    var view = (UnityEngine.Transform)Get(player, "CameraTransform");
    var to = at - view.position;
    float yaw = UnityEngine.Mathf.Atan2(to.x, to.z) * UnityEngine.Mathf.Rad2Deg;
    float pitch = -UnityEngine.Mathf.Atan2(to.y, new UnityEngine.Vector2(to.x, to.z).magnitude) * UnityEngine.Mathf.Rad2Deg;
    pitch = UnityEngine.Mathf.Clamp(pitch, -85f, 85f);
    Set(player, "_yaw", yaw);
    Set(player, "_xRotation", pitch);
    view.localRotation = UnityEngine.Quaternion.Euler(pitch, yaw, 0f);
}

bool StoneBetween(UnityEngine.Vector3 from, UnityEngine.Vector3 to)
{
    // As the Ophan judges it: any part of the built castle is stone, except a scorched room.
    var castle = T("Plunderspell.Castle.ProceduralCastleGenerator");
    var scorched = T("Plunderspell.Raid.ScorchedRooms");
    foreach (var hit in UnityEngine.Physics.RaycastAll(from, (to - from).normalized, (to - from).magnitude, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
        if (hit.collider.GetComponentInParent(castle) != null
            && !(bool)Call(scorched, "IsScorched", Call(scorched, "RoomOf", hit.collider))) return true;
    return false;
}

UnityEngine.Component Grab(UnityEngine.Component item)
{
    var player = LocalPlayer();
    var view = (UnityEngine.Transform)Get(player, "CameraTransform");
    var body = item.GetComponent<UnityEngine.Rigidbody>();
    var forward = view.forward; forward.y = 0f; forward.Normalize();
    var at = player.transform.position + forward * 1.8f + UnityEngine.Vector3.up * 1.0f;
    body.isKinematic = false;
    body.position = at;
    item.transform.position = at;
    body.linearVelocity = UnityEngine.Vector3.zero;
    Look(player, body.worldCenterOfMass);
    Call(Items(), "StartDragging", item, body.worldCenterOfMass);
    return item;
}

switch (action)
{
    case "huntoff":
        T("Plunderspell.Raid.OphanOfHours").GetField("HuntOverride").SetValue(null, (bool?)false);
        return "hunts off on this machine";

    case "noguards":
    {
        // Host: guards would hurt the players the check is measuring the beams on. A server destroy despawns
        // each one for every machine.
        int n = 0;
        foreach (UnityEngine.Component guard in FindAll("Plunderspell.Guards.Guard")) { UnityEngine.Object.Destroy(guard.gameObject); n++; }
        return "removed " + n + " guards";
    }

    case "huntauto":
        T("Plunderspell.Raid.OphanOfHours").GetField("HuntOverride").SetValue(null, null);
        return "hunts back to the raid's own switch";

    case "arrive":
        Call(Ophan(), "ArriveNow");
        return "arrive asked; state " + Get(Ophan(), "State");

    case "read":
    {
        var sb = new System.Text.StringBuilder();
        var ophan = Ophan();
        var look = Get(ophan, "Look") as UnityEngine.Component;
        sb.Append("hostState " + Get(ophan, "State"));
        if (look != null)
            sb.Append(" shown " + Get(look, "State") + " target " + ((Get(look, "Target") as UnityEngine.Transform)?.name ?? "none")
                + " beaming " + Get(look, "Beaming") + " eye " + V((UnityEngine.Vector3)Get(look, "EyePoint"))
                + " size " + (look.transform.lossyScale.y * 9.6f).ToString("F0") + "m");
        else
            sb.Append(" shown none");
        var lifted = UnityEngine.GameObject.Find("TakenPiece");
        if (lifted != null) sb.Append(" lifted " + V(lifted.transform.position) + " x" + lifted.transform.localScale.x.ToString("F1"));
        sb.Append(" | scorched " + Get(T("Plunderspell.Raid.ScorchedRooms"), "Count") + " | ");
        foreach (UnityEngine.Component p in FindAll("StateMachine.PlayerStateMachine"))
            sb.Append("player local=" + Get(p, "IsLocal") + " health=" + ((float)Get(p, "CurrentHealth")).ToString("F0")
                + " dead=" + Get(p, "dead") + " at=" + V(p.transform.position) + " | ");
        var held = Items() != null ? Get(Items(), "CarriedItem") as UnityEngine.Component : null;
        var value = held != null ? held.GetComponent(T("Plunderspell.Loot.LootValue")) : null;
        var lootItem = value != null ? Get(value, "Item") : null;
        sb.Append("holding " + (held != null ? held.name + " artifact=" + (lootItem != null && (bool)Get(lootItem, "IsArtifact")) : "nothing"));
        return sb.ToString();
    }

    case "face":
        Look(LocalPlayer(), EyeHere());
        return "facing the Ophan";

    case "place":
    {
        bool open = arg == "open";
        // The host judges cover from the point the hunt aims from, not the eye bone (it bobs and turns, at 150 times
        // its size tens of metres off); a client only has the eye bone.
        var eye = (bool)Get(Ophan(), "isServer") ? (UnityEngine.Vector3)Get(Ophan(), "_eyePoint") : EyeHere();
        var player = LocalPlayer();
        var body = player.GetComponent<UnityEngine.Rigidbody>();
        // The castle has its own navigation grid, not a Unity NavMesh: find floors by casting down over every room.
        var start = player.transform.position;
        UnityEngine.Vector3? best = null;
        float bestDistance = float.MaxValue;
        var floors = new System.Collections.Generic.List<UnityEngine.Vector3>();
        foreach (UnityEngine.Component room in FindAll("Plunderspell.Castle.CastleRoomModule"))
            for (float dx = -4.5f; dx <= 4.5f; dx += 1.5f)
            for (float dz = -4.5f; dz <= 4.5f; dz += 1.5f)
            {
                var top = new UnityEngine.Vector3(room.transform.position.x + dx, 3f, room.transform.position.z + dz);
                if (UnityEngine.Physics.Raycast(top, UnityEngine.Vector3.down, out var floor, 6f, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
                    floors.Add(floor.point);
            }
        // Never on the portal: standing in it carries the party home and ends the raid.
        var portal = UnityEngine.Object.FindFirstObjectByType(T("Plunderspell.Extraction.ExtractionZone")) as UnityEngine.Component;
        foreach (var c in floors)
        {
            if (c.y < -1f || c.y > 1.5f) continue;   // the ground floor only
            if (portal != null && (c - portal.transform.position).sqrMagnitude < 10f * 10f) continue;
            // The Ophan aims 1.2 m above the body's pivot, which settles about 0.9 m over the floor: judge the line at
            // that height and at chest height, so a spot is open or covered the same way to it.
            var head = c + UnityEngine.Vector3.up * 2.1f;
            var chest = c + UnityEngine.Vector3.up * 1.4f;
            if (open ? (StoneBetween(eye, head) || StoneBetween(eye, chest)) : !(StoneBetween(eye, head) && StoneBetween(eye, chest))) continue;
            // Cover: a room's stone close by on the Ophan's side (not a whole castle away, and a room so the charged
            // beam has something to scorch).
            if (!open && !(UnityEngine.Physics.Raycast(head, (eye - head).normalized, out var near, 6f, ~0, UnityEngine.QueryTriggerInteraction.Ignore)
                && Call(T("Plunderspell.Raid.ScorchedRooms"), "RoomOf", near.collider) != null)) continue;
            float d = (c - start).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = c; }
        }
        if (best == null) return "no " + arg + " spot found";
        // Moving the player drops what they carry: let go first and take it up again after.
        var carried = Items() != null ? Get(Items(), "CarriedItem") as UnityEngine.Component : null;
        if (carried != null) Call(Items(), "ForceRelease");
        var at = best.Value + UnityEngine.Vector3.up * 1.25f;
        body.position = at;
        player.transform.position = at;
        body.linearVelocity = UnityEngine.Vector3.zero;
        Look(player, eye);
        if (carried != null) { Grab(carried); Look(player, eye); }
        return "placed " + arg + " at " + V(at) + (carried != null ? " still carrying " + carried.name : "");
    }

    case "grabartifact":
    case "grabany":
    {
        bool wantArtifact = action == "grabartifact";
        UnityEngine.Component best = null;
        float bestWorth = -1f;
        foreach (UnityEngine.Component value in FindAll("Plunderspell.Loot.LootValue"))
        {
            var lootItem = Get(value, "Item");
            bool artifact = lootItem != null && (bool)Get(lootItem, "IsArtifact");
            var item = value.GetComponent(T("Item"));
            var rb = value.GetComponent<UnityEngine.Rigidbody>();
            if (item == null || rb == null || artifact != wantArtifact || rb.mass > 12f) continue;
            float worth = (float)Get(value, "Worth");
            if (worth > bestWorth) { bestWorth = worth; best = item; }
        }
        if (best == null) return "no " + (wantArtifact ? "artifact" : "piece") + " to grab";
        Grab(best);
        return "grabbed " + best.name + " worth " + bestWorth.ToString("F0");
    }

    case "release":
        Call(Items(), "ForceRelease");
        return "released";

    case "roomshot":
    {
        var scorched = (System.Collections.IEnumerable)T("Plunderspell.Raid.ScorchedRooms").GetField("s_scorched", All).GetValue(null);
        UnityEngine.Component room = null;
        foreach (UnityEngine.Component r in scorched) { room = r; break; }
        if (room == null) return "no scorched room";
        var camGo = new UnityEngine.GameObject("ScorchShotCamera");
        var cam = camGo.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = 60f;
        cam.farClipPlane = 3000f;
        var rt = new UnityEngine.RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        var centre = room.transform.position;
        cam.transform.position = centre + new UnityEngine.Vector3(10f, 14f, -10f);
        cam.transform.LookAt(centre);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(arg, UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.RenderTexture.active = null;
        UnityEngine.Object.Destroy(camGo);
        rt.Release();
        return "room shot of " + room.name + " saved " + arg;
    }

    case "liftshot":
    {
        var lifted = UnityEngine.GameObject.Find("TakenPiece");
        if (lifted == null) return "no piece being lifted";
        var camGo = new UnityEngine.GameObject("LiftShotCamera");
        var cam = camGo.AddComponent<UnityEngine.Camera>();
        cam.fieldOfView = 50f;
        cam.farClipPlane = 3000f;
        var rt = new UnityEngine.RenderTexture(1600, 900, 24);
        cam.targetTexture = rt;
        var up = (EyeHere() - lifted.transform.position).normalized;
        cam.transform.position = lifted.transform.position - up * 25f + UnityEngine.Vector3.right * 6f;
        cam.transform.LookAt(lifted.transform.position);
        cam.Render();
        UnityEngine.RenderTexture.active = rt;
        var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
        tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0);
        tex.Apply();
        System.IO.File.WriteAllBytes(arg, UnityEngine.ImageConversion.EncodeToPNG(tex));
        UnityEngine.RenderTexture.active = null;
        UnityEngine.Object.Destroy(camGo);
        rt.Release();
        return "lift shot at " + V(lifted.transform.position) + " saved " + arg;
    }

    case "slow":
        UnityEngine.Time.timeScale = float.Parse(arg, System.Globalization.CultureInfo.InvariantCulture);
        return "time scale " + UnityEngine.Time.timeScale;

    case "shot":
        UnityEngine.ScreenCapture.CaptureScreenshot(arg);
        return "shot requested " + arg;
}
return "unknown action " + action;

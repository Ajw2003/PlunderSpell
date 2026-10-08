// Play mode only, in a raid (#259). ACTION "spot": brings the other ground-floor guards to 10 m from the living guard
// nearest the ground-floor centre, then stands the local player 6 m from that guard on a spot it can see, and turns
// the guard to it, so exactly one guard sees them first and the others can only come because it cries. ACTION "sample": one line: game state, player health, alarm state, level,
// witnesses, chasers, how many guards are patrolling, investigating, chasing or otherwise, and what the spotter sees.
// "sample" also tops the player's health back up. Run "sample" every second or so after "spot". Driven by Tools/Unity/alarm_witness_check.sh.
string action = "__ACTION__";
var director = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Alarm.EnemyDirector>();
var guards = new System.Collections.Generic.List<Plunderspell.Guards.Guard>();
foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
    if (g != null && g.CurrentHealth > 0f) guards.Add(g);
if (action == "spot")
{
    Plunderspell.Guards.Guard pick = null;
    float best = float.MaxValue;
    foreach (var g in guards)
    {
        var at = g.transform.position;
        if (at.y > 3f || at.y < -1f) continue;
        float d = new UnityEngine.Vector2(at.x, at.z).magnitude;
        if (d < best) { best = d; pick = g; }
    }
    if (pick == null) return "no ground-floor guard";
    // Brings the other ground-floor guards to 10 m from the spotter, on open floor with no wall between, so its cry (18 m, GuardCry.Radius)
    // can reach them: on seed 777 they patrol too far apart for that.
    int brought = 0;
    for (int k = 0; k < guards.Count; k++)
    {
        var o = guards[k];
        if (o == pick || UnityEngine.Mathf.Abs(o.transform.position.y - pick.transform.position.y) > 1.5f) continue;
        for (int i = 0; i < 16; i++)
        {
            var dir = UnityEngine.Quaternion.Euler(0f, 180f + (brought * 90f) + i * 22.5f, 0f) * pick.transform.forward;
            var feet = pick.transform.position + dir * 10f;
            if (!UnityEngine.Physics.Raycast(feet + UnityEngine.Vector3.up * 1.5f, UnityEngine.Vector3.down, out var floorHit, 3f, pick.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
            if (UnityEngine.Mathf.Abs(floorHit.point.y - pick.transform.position.y) > 0.5f) continue;
            if (UnityEngine.Physics.CheckSphere(floorHit.point + UnityEngine.Vector3.up * 1f, 0.5f, pick.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
            // No wall between them: walls muffle a cry (GuardCry.Strength), and this check is about the witnesses.
            if (UnityEngine.Physics.Linecast(pick.transform.position + UnityEngine.Vector3.up * 1.5f, floorHit.point + UnityEngine.Vector3.up * 1.5f, pick.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
            o.transform.position = floorHit.point;
            o.Leads.Clear();
            o.ChangeState(o.States.Patrol);
            System.AppDomain.CurrentDomain.SetData("awBrought" + brought, o);
            brought++;
            break;
        }
    }
    var p = StateMachine.PlayerStateMachine.Local;
    // A spot 6 m from the guard with floor under it and a clear line from the guard's eye to the player's head,
    // tried in 16 directions: a fixed "in front" spot landed behind a tree and a stable on seed 777.
    var eyeAt = pick.transform.position + UnityEngine.Vector3.up * pick.Tuning.EyeHeight;
    UnityEngine.Vector3 stand = UnityEngine.Vector3.zero;
    bool found = false;
    for (int i = 0; i < 16 && !found; i++)
    {
        var dir = UnityEngine.Quaternion.Euler(0f, i * 22.5f, 0f) * pick.transform.forward;
        var feet = pick.transform.position + dir * 6f;
        if (!UnityEngine.Physics.Raycast(feet + UnityEngine.Vector3.up * 1.5f, UnityEngine.Vector3.down, out var floor, 3f, pick.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
        if (UnityEngine.Mathf.Abs(floor.point.y - pick.transform.position.y) > 0.5f) continue;
        // The player pivot sits 1.19 m above its feet.
        var candidate = floor.point + UnityEngine.Vector3.up * 1.25f;
        var head = candidate + UnityEngine.Vector3.up * pick.Tuning.TargetAimHeight;
        if (UnityEngine.Physics.Linecast(eyeAt, head, pick.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
        stand = candidate; found = true;
    }
    if (!found) return "no clear spot 6 m round " + pick.name;
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    rb.position = stand; p.transform.position = stand; rb.linearVelocity = UnityEngine.Vector3.zero;
    var look = pick.transform.position - stand;
    p.FaceYaw(UnityEngine.Mathf.Atan2(look.x, look.z) * UnityEngine.Mathf.Rad2Deg);
    pick.transform.rotation = UnityEngine.Quaternion.LookRotation(-new UnityEngine.Vector3(look.x, 0f, look.z), UnityEngine.Vector3.up);
    pick.Sight.LookNext();
    UnityEngine.Physics.SyncTransforms();
    System.AppDomain.CurrentDomain.SetData("awSpotter", pick);
    var heard0 = new System.Collections.Generic.Dictionary<Plunderspell.Guards.Guard, int>();
    string neighbours = "";
    foreach (var o in guards)
    {
        heard0[o] = o.Hearing.NoticedCount;
        float d = (o.transform.position - pick.transform.position).magnitude;
        if (o != pick && d < 25f) neighbours += " " + o.name + " " + d.ToString("F1") + " m";
    }
    System.AppDomain.CurrentDomain.SetData("awHeard0", heard0);
    // Every cry heard, state change and blocked walk from now on, so a guard that does not come over shows why.
    var blocks = new System.Collections.Generic.List<string>();
    System.AppDomain.CurrentDomain.SetData("awBlocks", blocks);
    foreach (var o in guards)
    {
        var listener = o;
        listener.StateChanged += st => blocks.Add(listener.name + " -> " + st + " at " + UnityEngine.Time.time.ToString("F2") + " pos " + listener.transform.position.ToString("F1"));
        listener.Navigator.RouteBlocked += b => blocks.Add(listener.name + " route blocked " + b.Reason + " at " + UnityEngine.Time.time.ToString("F2"));
        listener.Hearing.CryHeard += from => blocks.Add(listener.name + " heard a cry from " + from.ToString("F1") + " at " + UnityEngine.Time.time.ToString("F2") + " (it is at " + listener.transform.position.ToString("F1") + ", " + listener.State + ")");
    }
    director.OnBlocked += b => blocks.Add(b.Guard.name + " " + b.Reason + " at " + b.Position.ToString("F1"));
    return "brought " + brought + " guards to 10 m; spotter " + pick.name + " at " + pick.transform.position.ToString("F1") + "; within 25 m:" + neighbours;
}
var spotter = System.AppDomain.CurrentDomain.GetData("awSpotter") as Plunderspell.Guards.Guard;
string watch = "";
if (spotter != null)
{
    var me = StateMachine.PlayerStateMachine.Local.transform.position;
    var to = me - spotter.transform.position;
    var eye = spotter.transform.position + UnityEngine.Vector3.up * spotter.Tuning.EyeHeight;
    var head = me + UnityEngine.Vector3.up * spotter.Tuning.TargetAimHeight;
    string ray = "clear";
    foreach (var h in UnityEngine.Physics.RaycastAll(eye, (head - eye).normalized, (head - eye).magnitude, spotter.Tuning.GeometryLayers, UnityEngine.QueryTriggerInteraction.Ignore))
        if (!h.collider.transform.IsChildOf(StateMachine.PlayerStateMachine.Local.transform)) { ray = "blocked by " + h.collider.name; break; }
    watch = " | spotter " + spotter.State + " sees " + (spotter.Sight.Visible != null) + " dist " + to.magnitude.ToString("F1")
        + " angle " + UnityEngine.Vector3.Angle(spotter.transform.forward, new UnityEngine.Vector3(to.x, 0f, to.z)).ToString("F0")
        + " intruders " + director.Intruders.Count + " ray " + ray
        + " grace " + Plunderspell.Guards.GuardArrivalGrace.IsActive + " player " + me.ToString("F1");
}
int heard = 0;
var heardAt = System.AppDomain.CurrentDomain.GetData("awHeard0") as System.Collections.Generic.Dictionary<Plunderspell.Guards.Guard, int>;
if (heardAt != null) foreach (var g in guards) if (g != spotter && heardAt.TryGetValue(g, out int n0) && g.Hearing.NoticedCount > n0) heard++;
string broughtStates = "";
for (int b = 0; b < 2; b++)
    if (System.AppDomain.CurrentDomain.GetData("awBrought" + b) is Plunderspell.Guards.Guard bg)
        broughtStates += " " + bg.State + " " + (spotter != null ? (bg.transform.position - spotter.transform.position).magnitude.ToString("F1") + " m" : "");
string investigators = "";
foreach (var g in guards)
    if (g.State == Plunderspell.Guards.GuardAlertState.Investigating)
        investigators += " " + g.name + " at " + g.transform.position.ToString("F1") + (spotter != null ? " " + (g.transform.position - spotter.transform.position).magnitude.ToString("F1") + " m from spotter" : "") + " going to " + ((Plunderspell.Guards.InvestigateState)g.States.Investigate).Spot.ToString("F1");
var blockList = System.AppDomain.CurrentDomain.GetData("awBlocks") as System.Collections.Generic.List<string>;
string blocked = blockList != null && blockList.Count > 0 ? " | events: " + string.Join("; ", blockList) : "";
if (blockList != null) blockList.Clear();
int patrol = 0, investigate = 0, chase = 0, other = 0;
foreach (var g in guards)
{
    switch (g.State)
    {
        case Plunderspell.Guards.GuardAlertState.Patrolling: patrol++; break;
        case Plunderspell.Guards.GuardAlertState.Investigating: investigate++; break;
        case Plunderspell.Guards.GuardAlertState.Chasing: chase++; break;
        default: other++; break;
    }
}
var alarm = typeof(Plunderspell.Alarm.EnemyDirector).GetProperty("Alarm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(director);
int witnesses = (int)alarm.GetType().GetProperty("Witnesses").GetValue(alarm);
var local = StateMachine.PlayerStateMachine.Local;
// Tops the player's health back up every sample, so the player survives long enough for a third witness to arrive.
if (local != null && local.IsAlive)
    typeof(StateMachine.PlayerStateMachine).GetField("_health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(local, local.MaxHealth);
return Plunderspell.Core.GameServices.GameState.CurrentState + " hp " + (local != null ? local.CurrentHealth.ToString("F0") : "none") + " | " + director.State + " level " + director.AlarmLevel.ToString("F0") + " witnesses " + witnesses
    + " chasers " + director.ChasingGuards + " | guards patrol " + patrol + " investigate " + investigate + " chase " + chase + " other " + other + " heard since spot " + heard + (broughtStates.Length > 0 ? " | brought:" + broughtStates : "") + watch + (investigators.Length > 0 ? " | investigating:" + investigators : "") + blocked;

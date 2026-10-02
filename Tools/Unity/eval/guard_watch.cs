// Play mode, host side only (the server drives the guards). Measures how long guards stand still
// with nowhere to go, for Tools/Unity/coop_guard_check.sh, which replaces the placeholder below.
//   provoke   point every guard at the local player's spot and make it chase, so each loses sight
//             and searches: the situation #188/#189/#190 are about
//   sample    add the time since the last sample to each guard's counters
//   report    one line of totals, then one line per guard
//   dump      one line per stuck sample (position, state, destination, velocities, ground normal, what it
//             touches), for the cause analysis in #200
//   levo / levocheck / frango / frangocheck   host-side checks of Levo and the Frango shove
// Each sample, per live guard that is not incapacitated, takes the ground it covered since the last
// sample and classes the interval:
//   stuck    has a destination farther than 1 m away, moved under 0.2 m per second
//   parked   has a destination it is already at, moved under 0.2 m per second (waiting there)
//   nodest   has no destination and did not move
//   moving   anything else
// "standing" is stuck + parked + nodest. State counts are taken at each sample.
string action = "__ACTION__";
var All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var guardType = System.AppDomain.CurrentDomain.GetAssemblies()
    .Select(a => a.GetType("Plunderspell.Guards.CastleGuard")).First(t => t != null);
var active = (System.Collections.IEnumerable)guardType.GetProperty("Active").GetValue(null);
var data = System.AppDomain.CurrentDomain.GetData("guardWatch") as System.Collections.Generic.Dictionary<int, double[]>;
if (data == null)
{
    data = new System.Collections.Generic.Dictionary<int, double[]>();
    System.AppDomain.CurrentDomain.SetData("guardWatch", data);
}
// per guard: 0 lastX 1 lastZ 2 lastTime 3 stuck 4 parked 5 nodest 6 moving 7 observed
var rows = System.AppDomain.CurrentDomain.GetData("guardWatchRows") as System.Collections.Generic.List<string>;
if (rows == null)
{
    rows = new System.Collections.Generic.List<string>();
    System.AppDomain.CurrentDomain.SetData("guardWatchRows", rows);
}
var playerTypeW = System.AppDomain.CurrentDomain.GetAssemblies()
    .Select(a => a.GetType("StateMachine.PlayerStateMachine")).First(t => t != null);
var states = new System.Collections.Generic.Dictionary<string, int>();

if (action == "provoke")
{
    var playerType = System.AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => a.GetType("StateMachine.PlayerStateMachine")).First(t => t != null);
    var local = (UnityEngine.Component)playerType.GetProperty("Local").GetValue(null);
    var spot = local.transform.position;
    int n = 0;
    var stateType = guardType.Assembly.GetType("Plunderspell.Guards.GuardAlertState");
    foreach (var g in active)
    {
        var guard = (UnityEngine.Component)g;
        guardType.GetField("_lastKnownIntruderPosition", All).SetValue(guard, spot);
        guardType.GetMethod("SetAlertState").Invoke(guard, new object[] { System.Enum.Parse(stateType, "Chasing") });
        n++;
    }
    return "provoked " + n + " guards toward " + spot;
}

if (action == "dump")
    return string.Join(System.Environment.NewLine, rows);

if (action == "levo" || action == "levocheck" || action == "frango" || action == "frangocheck")
{
    var key = action.StartsWith("levo") ? "levoGuard" : "frangoGuard";
    UnityEngine.Component target = System.AppDomain.CurrentDomain.GetData(key) as UnityEngine.Component;
    if (action == "levo" || action == "frango")
    {
        foreach (var g in active)
        {
            var c = (UnityEngine.Component)g;
            if (guardType.GetProperty("State").GetValue(c).ToString() == "Incapacitated") continue;
            target = c;
            if (action == "levo" && c.GetInstanceID() != 0) break;
            if (action == "frango") break;
        }
        System.AppDomain.CurrentDomain.SetData(key, target);
        System.AppDomain.CurrentDomain.SetData(key + "Pos", target.transform.position);
        if (action == "levo")
        {
            var recv = target.GetComponent("StatusEffectReceiver");
            recv.GetType().GetMethod("Levitate").Invoke(recv, new object[] { UnityEngine.Vector3.up * 4f, 2.5f, null });
        }
        else
            ((Interfaces.IShovable)target).Shove(target.transform.forward * 2f);
        return action + " started on guard " + target.GetInstanceID() + " at " + target.transform.position;
    }
    var from = (UnityEngine.Vector3)System.AppDomain.CurrentDomain.GetData(key + "Pos");
    var ag = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
    var rbd = target.GetComponent<UnityEngine.Rigidbody>();
    var air = (bool)guardType.GetProperty("IsAirborne").GetValue(target);
    var dd = target.transform.position - from;
    System.AppDomain.CurrentDomain.SetData(key + "Pos", target.transform.position);
    return action + " airborne " + air + " onMesh " + ag.isOnNavMesh + " kinematic " + rbd.isKinematic
        + " y " + target.transform.position.y.ToString("F2") + " movedSinceLast " + dd.magnitude.ToString("F2")
        + " (xz " + new UnityEngine.Vector2(dd.x, dd.z).magnitude.ToString("F2") + ") state "
        + guardType.GetProperty("State").GetValue(target);
}

double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
int live = 0;
foreach (var g in active)
{
    var guard = (UnityEngine.Component)g;
    int id = guard.GetInstanceID();
    var st = guardType.GetProperty("State").GetValue(guard).ToString();
    states[st] = states.ContainsKey(st) ? states[st] + 1 : 1;
    if (!data.TryGetValue(id, out var row))
    {
        row = new double[8];
        row[0] = guard.transform.position.x; row[1] = guard.transform.position.z; row[2] = now;
        data[id] = row;
        continue;
    }
    if (action != "sample") continue;
    double dt = now - row[2];
    if (dt < 0.2) continue;
    var p = guard.transform.position;
    double moved = System.Math.Sqrt((p.x - row[0]) * (p.x - row[0]) + (p.z - row[1]) * (p.z - row[1]));
    row[0] = p.x; row[1] = p.z; row[2] = now;
    if (st == "Incapacitated") continue;
    live++;
    var dest = (UnityEngine.Vector3?)guardType.GetProperty("Destination").GetValue(guard);
    bool slow = moved < 0.2 * dt;
    if (!slow) { row[6] += dt; }
    else if (!dest.HasValue) { row[5] += dt; }
    else
    {
        var d = dest.Value;
        double far = System.Math.Sqrt((p.x - d.x) * (p.x - d.x) + (p.z - d.z) * (p.z - d.z));
        var ag = guard.GetComponent<UnityEngine.AI.NavMeshAgent>();
        var rb = guard.GetComponent<UnityEngine.Rigidbody>();
        // Stuck only when the body is really stopped (under 0.2 m/s) while the agent wants to move.
        bool bodyStopped = rb == null || new UnityEngine.Vector2(rb.linearVelocity.x, rb.linearVelocity.z).magnitude < 0.2f;
        bool wants = ag != null && ag.enabled && new UnityEngine.Vector2(ag.desiredVelocity.x, ag.desiredVelocity.z).magnitude > 0.01f;
        if (far > 1.0 && !(bodyStopped && wants)) row[6] += dt;
        else if (far > 1.0)
        {
            row[3] += dt;
            var cap = guard.GetComponent<UnityEngine.CapsuleCollider>();
            string ground = "none";
            if (UnityEngine.Physics.Raycast(p + UnityEngine.Vector3.up * 0.5f, UnityEngine.Vector3.down, out var gh, 2f))
                ground = gh.collider.name + " n=" + gh.normal.ToString("F2") + " slope=" + UnityEngine.Vector3.Angle(gh.normal, UnityEngine.Vector3.up).ToString("F0");
            var touching = new System.Collections.Generic.List<string>();
            float rad = (cap != null ? cap.radius : 0.4f) + 0.1f;
            var lo = p + UnityEngine.Vector3.up * (rad + 0.05f);
            var hi = p + UnityEngine.Vector3.up * ((cap != null ? cap.height : 1.8f) - rad);
            foreach (var col in UnityEngine.Physics.OverlapCapsule(lo, hi, rad, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
            {
                if (col.transform.root == guard.transform.root) continue;
                string kind;
                if (col.GetComponentInParent(guardType) != null) kind = "GUARD";
                else if (col.GetComponentInParent(playerTypeW) != null) kind = "PLAYER";
                else if (col.attachedRigidbody != null) kind = "PROP";
                else kind = "STATIC";
                touching.Add(kind + ":" + col.name + "/L" + UnityEngine.LayerMask.LayerToName(col.gameObject.layer));
            }
            rows.Add("guard " + guard.GetInstanceID() + " t " + now.ToString("F0") + " dt " + dt.ToString("F1") + " pos " + p.ToString("F2")
                + " state " + st + " dest " + d.ToString("F2") + " distDest " + far.ToString("F1")
                + " vel " + (rb != null ? rb.linearVelocity.ToString("F2") : "nobody") + (rb != null && rb.isKinematic ? "(kin)" : "")
                + " desired " + (ag != null && ag.enabled ? ag.desiredVelocity.ToString("F2") : "noagent")
                + " onMesh " + (ag != null && ag.enabled && ag.isOnNavMesh) + " agentGap " + (ag != null && ag.enabled ? UnityEngine.Vector3.Distance(ag.nextPosition, rb != null ? rb.position : p).ToString("F2") : "n/a")
                + " ground " + ground + " touching [" + string.Join(", ", touching) + "]");
        }
        else row[4] += dt;
    }
    row[7] += dt;
}
if (action == "sample")
    return "sampled " + live + " " + string.Join(" ", states.Select(kv => kv.Key + "=" + kv.Value));

double stuck = 0, parked = 0, nodest = 0, moving = 0, total = 0;
var lines = new System.Text.StringBuilder();
foreach (var kv in data)
{
    var r = kv.Value;
    stuck += r[3]; parked += r[4]; nodest += r[5]; moving += r[6]; total += r[7];
    lines.Append("\nguard " + kv.Key + " stuck " + r[3].ToString("F1") + " parked " + r[4].ToString("F1")
        + " nodest " + r[5].ToString("F1") + " moving " + r[6].ToString("F1"));
}
return "TOTAL guards " + data.Count + " observed_s " + total.ToString("F1") + " stuck_s " + stuck.ToString("F1")
    + " parked_s " + parked.ToString("F1") + " nodest_s " + nodest.ToString("F1") + " moving_s " + moving.ToString("F1")
    + " standing_s " + (stuck + parked + nodest).ToString("F1")
    + " now " + string.Join(" ", states.Select(kv => kv.Key + "=" + kv.Value)) + lines;

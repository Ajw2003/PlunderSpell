// Play mode, host side only (the server drives the guards). Measures how long guards stand still
// with nowhere to go, for Tools/Unity/coop_guard_check.sh, which replaces the placeholder below.
//   provoke   point every guard at the local player's spot and make it chase, so each loses sight
//             and searches: the situation #188/#189/#190 are about
//   sample    add the time since the last sample to each guard's counters
//   report    one line of totals, then one line per guard
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
        if (far > 1.0) row[3] += dt; else row[4] += dt;
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

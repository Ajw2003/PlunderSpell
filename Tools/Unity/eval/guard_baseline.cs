// Play mode, host side (the server runs the guards). Stages the five guard scenarios from aj's 2026-10-28 playtest
// (#480) and samples the guards in them, for Tools/Unity/guard_baseline_check.sh, which replaces __ACTION__/__ARG__.
//   prep            ends the arrival grace so a calm castle can see
//   snap            remembers which map cells are walkable now (before the Ophan breaks anything)
//   holes           cells walkable at the snap that are not now and have no floor: the beam's holes; prints one
//   stage <name>    beside | spot | pillar | trio | hole: places the subject guard(s) and the host player, starts it
//   hide            pillar only: moves the player to the nearest floor the subject guard cannot see
//   sample          one line per subject guard: seconds, state, position, yaw, who it sees, destination, strikes
// The host player is held still (kinematic) and turned to face the subject, so the host window shows the guard.
string action = "__ACTION__";
string arg = "__ARG__";
const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static
    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var domain = System.AppDomain.CurrentDomain;
var player = StateMachine.PlayerStateMachine.Local;
if (player == null) return "no local player";
var director = Plunderspell.Alarm.EnemyDirector.Current;
var generator = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Castle.ProceduralCastleGenerator>();
var graph = generator != null && generator.LastGenerated != null ? generator.LastGenerated.NavGraph : null;
var grid = graph == null ? null : (Plunderspell.Castle.CastleNavGrid)typeof(Plunderspell.Castle.CastleNavGraph).GetField("_grid", All).GetValue(graph);
var map = director != null ? director.Navigation.Map : null;
var path = new System.Collections.Generic.List<UnityEngine.Vector3>();

System.Collections.Generic.List<Plunderspell.Guards.Guard> Live()
{
    var list = new System.Collections.Generic.List<Plunderspell.Guards.Guard>();
    foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
        if (g != null && !g.IsDead && !g.Status.IsIncapacitated) list.Add(g);
    list.Sort((a, b) => (a.transform.position - player.transform.position).sqrMagnitude
        .CompareTo((b.transform.position - player.transform.position).sqrMagnitude));
    return list;
}

void Hold(UnityEngine.Vector3 at, UnityEngine.Vector3 lookAt)
{
    var rb = player.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = true;
    var flat = lookAt - at; flat.y = 0f;
    if (flat.sqrMagnitude > 0.01f) player.FaceYaw(UnityEngine.Quaternion.LookRotation(flat).eulerAngles.y);
    rb.position = at; player.transform.position = at;
    UnityEngine.Physics.SyncTransforms();
}

void Place(Plunderspell.Guards.Guard g, UnityEngine.Vector3 at, UnityEngine.Vector3 facing)
{
    g.Navigator.Stop();
    g.transform.position = at;
    facing.y = 0f;
    if (facing.sqrMagnitude > 0.01f) g.transform.rotation = UnityEngine.Quaternion.LookRotation(facing);
    UnityEngine.Physics.SyncTransforms();
    g.Sight.LookNext(); // the last look was taken from where it stood before; look again from here at once
}

void Chase(Plunderspell.Guards.Guard g)
{
    g.States.Chase.Follow(player.transform);
    g.ChangeState(g.States.Chase);
}

// The nearest walkable floor to a point on the same level (within 0.6 m of its height), searching out to 3 m, or the
// point itself when there is none. A plain nearest cell can be on the floor above or below.
UnityEngine.Vector3 Floor(UnityEngine.Vector3 p)
{
    for (float r = 0f; r <= 3f; r += 0.5f)
        for (int k = 0; k < (r == 0f ? 1 : 12); k++)
        {
            float a = k * UnityEngine.Mathf.PI / 6f;
            var probe = p + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a)) * r;
            int cell = graph.NearestWalkableCell(probe, 0.6f);
            if (cell >= 0 && UnityEngine.Mathf.Abs(graph.CellPosition(cell).y - p.y) < 0.6f) return graph.CellPosition(cell);
        }
    return p;
}

// Where the host player's feet are (the pivot is the capsule centre, 0.9 m up).
UnityEngine.Vector3 Feet() => player.transform.position - UnityEngine.Vector3.up * 0.9f;

// The guards in the scenario; every other guard is stunned for 40 s so only the subjects act.
void Subjects(params Plunderspell.Guards.Guard[] guards)
{
    foreach (var other in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
        if (other != null && !other.IsDead && System.Array.IndexOf(guards, other) < 0) other.Status.Stun(40f);
    domain.SetData("gb.subjects", guards);
    domain.SetData("gb.start", UnityEngine.Time.time);
}

if (action == "prep")
{
    Plunderspell.Guards.GuardArrivalGrace.End();
    domain.SetData("gb.home", player.transform.position);
    return "grace ended; live guards " + Live().Count + "; map " + (graph != null);
}

if (action == "snap")
{
    int total = grid.ModuleCount * Plunderspell.Castle.CastleNavGrid.CellsPerModule;
    var walkable = new bool[total];
    for (int c = 0; c < total; c++) walkable[c] = grid.IsWalkable(c);
    domain.SetData("gb.snap", walkable);
    return "snap " + grid.WalkableCellCount + " walkable of " + total;
}

if (action == "holes")
{
    var before = (bool[])domain.GetData("gb.snap");
    if (before == null) return "no snap";
    var physics = UnityEngine.Physics.defaultPhysicsScene;
    int gone = 0; UnityEngine.Vector3 first = UnityEngine.Vector3.zero;
    var holes = new System.Collections.Generic.List<UnityEngine.Vector3>();
    for (int c = 0; c < before.Length; c++)
    {
        if (!before[c] || grid.IsWalkable(c)) continue;
        var at = grid.CellPosition(c);
        if (Plunderspell.Castle.CastleNavProbe.HasFloor(physics, at)) continue;
        if (gone == 0) first = at;
        gone++; holes.Add(at);
    }
    domain.SetData("gb.holes", holes);
    return gone == 0 ? "holes 0" : "holes " + gone + " first " + first.ToString("F1");
}

if (action == "stage")
{
    // Wakes the guards the last scenario stunned, so any of them can be this one's subject.
    foreach (var g0 in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
        if (g0 != null && !g0.IsDead && g0.Status.IsStunned) g0.Status.ClearAll();
    // Every scenario starts with the host player back at the arrival spot, facing the same way.
    var home = (UnityEngine.Vector3)domain.GetData("gb.home");
    Hold(home, home + UnityEngine.Vector3.forward);
    var live = Live();
    if (live.Count == 0) return "no live guard";
    var me = player.transform.position;
    var feet = Feet();
    if (arg == "beside")
    {
        // A guard 2 m to the player's right, facing the way the player faces: the player is 90 degrees off its nose.
        var g = live[0];
        var at = Floor(feet + player.transform.right * 2f);
        Place(g, at, player.transform.forward);
        g.Leads.Offer(at, 0.5f);
        g.ChangeState(g.States.Investigate);
        Hold(me, at);
        Subjects(g);
        return "beside " + g.name + " at " + UnityEngine.Vector3.Distance(at, me).ToString("F1") + " m";
    }
    if (arg == "spot")
    {
        // The checked spot is 1.2 m in front of the nearest wall ahead of the player; the guard walks there from 7 m
        // back, and the player stands 2 m behind where the guard started, so the guard sees them only if it turns.
        var forward = player.transform.forward; forward.y = 0f; forward.Normalize();
        var eye = me + UnityEngine.Vector3.up;
        var spot = UnityEngine.Physics.Raycast(eye, forward, out UnityEngine.RaycastHit hit, 10f, 1)
            ? Floor(new UnityEngine.Vector3(hit.point.x, feet.y, hit.point.z) - forward * 1.2f) : Floor(feet + forward * 6f);
        var g = live[0];
        Place(g, Floor(spot - forward * 7f), forward);
        g.Leads.Offer(spot, 1f);
        g.ChangeState(g.States.Investigate);
        var stand = Floor(spot - forward * 9f);
        Hold(stand + UnityEngine.Vector3.up * 0.9f, g.transform.position);
        Subjects(g);
        return "spot " + spot.ToString("F1") + " guard " + g.name + " player " + stand.ToString("F1");
    }
    if (arg == "pillar")
    {
        var g = live[0];
        Place(g, Floor(feet + player.transform.forward * 6f), me - feet - player.transform.forward * 6f);
        Chase(g);
        Hold(me, g.transform.position);
        Subjects(g);
        return "pillar chase " + g.name;
    }
    if (arg == "trio")
    {
        var melee = live.FindAll(x => !x.RangedAttack.IsRanged);
        if (melee.Count < 3) return "trio needs 3 melee guards, have " + melee.Count;
        var three = melee.GetRange(0, 3).ToArray();
        for (int i = 0; i < 3; i++)
        {
            float a = i * UnityEngine.Mathf.PI * 2f / 3f;
            var at = Floor(feet + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a)) * 4f);
            Place(three[i], at, me - at);
            Chase(three[i]);
        }
        Hold(me, three[0].transform.position);
        Subjects(three);
        return "trio " + string.Join(", ", System.Array.ConvertAll(three, x => x.name));
    }
    if (arg == "hole")
    {
        // A guard and the player on floor either side of a beam hole, 3 to 9 m apart, with the hole between them.
        var holes = domain.GetData("gb.holes") as System.Collections.Generic.List<UnityEngine.Vector3>;
        if (holes == null || holes.Count == 0) return "no holes";
        UnityEngine.Vector3 centre = holes[0], bestA = UnityEngine.Vector3.zero, bestB = UnityEngine.Vector3.zero; bool found = false;
        for (int h = 0; h < holes.Count && !found; h++)
        for (int k = 0; k < 16 && !found; k++)
        {
            float a = k * UnityEngine.Mathf.PI / 8f;
            centre = holes[h];
            var dir = new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a));
            for (float r = 2f; r <= 4.5f && !found; r += 0.5f)
            {
                int ca = graph.NearestWalkableCell(centre - dir * r, 0.6f), cb = graph.NearestWalkableCell(centre + dir * r, 0.6f);
                if (ca < 0 || cb < 0) continue;
                var A = graph.CellPosition(ca); var B = graph.CellPosition(cb);
                if (UnityEngine.Mathf.Abs(A.y - B.y) > 0.5f || UnityEngine.Mathf.Abs(A.y - centre.y) > 0.5f) continue;
                bestA = A; bestB = B; found = true;
            }
        }
        if (!found) return "no floor both sides of the hole at " + centre.ToString("F1");
        bool routed = map.TryFindPath(bestA, bestB, path, out var why);
        var g = live[0];
        Place(g, bestA, bestB - bestA);
        Chase(g);
        Hold(bestB + UnityEngine.Vector3.up * 0.9f, bestA);
        Subjects(g);
        return "hole at " + centre.ToString("F1") + " guard " + bestA.ToString("F1") + " player " + bestB.ToString("F1")
            + " route " + (routed ? path.Count + " points" : why.ToString());
    }
    return "unknown stage " + arg;
}

if (action == "hide")
{
    // The nearest map floor within 12 m of the player, on its level, that the subject guard's eye has no clear line to.
    var g = ((Plunderspell.Guards.Guard[])domain.GetData("gb.subjects"))[0];
    var eye = g.transform.position + UnityEngine.Vector3.up * g.Tuning.EyeHeight;
    var me = player.transform.position;
    UnityEngine.Vector3 best = me; float bestDistance = float.MaxValue;
    for (int k = 0; k < 24; k++)
        for (float r = 2f; r <= 12f; r += 1f)
        {
            float a = k * UnityEngine.Mathf.PI / 12f;
            int cell = graph.NearestWalkableCell(me + new UnityEngine.Vector3(UnityEngine.Mathf.Cos(a), 0f, UnityEngine.Mathf.Sin(a)) * r, 0.6f);
            if (cell < 0) continue;
            var floor = graph.CellPosition(cell);
            if (UnityEngine.Mathf.Abs(floor.y - Feet().y) > 0.6f) continue;
            var head = floor + UnityEngine.Vector3.up * 1.9f;
            if (!UnityEngine.Physics.Linecast(eye, head, 1, UnityEngine.QueryTriggerInteraction.Ignore)) continue;
            float d = (floor - me).sqrMagnitude;
            if (d < bestDistance) { bestDistance = d; best = floor; }
        }
    if (bestDistance == float.MaxValue) return "no hidden floor near";
    Hold(best + UnityEngine.Vector3.up * 0.9f, g.transform.position);
    return "hid at " + best.ToString("F1") + ", " + UnityEngine.Mathf.Sqrt(bestDistance).ToString("F1") + " m";
}

if (action == "far")
{
    // A walkable floor at least 40 m from the host player, for parking the client's player out of every scenario.
    var me = player.transform.position;
    int total = grid.ModuleCount * Plunderspell.Castle.CastleNavGrid.CellsPerModule;
    for (int c = 0; c < total; c += 7)
        if (grid.IsWalkable(c) && (grid.CellPosition(c) - me).sqrMagnitude > 1600f)
        {
            var f = grid.CellPosition(c) + UnityEngine.Vector3.up * 0.9f;
            return f.x.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + f.y.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + f.z.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        }
    return "none";
}

if (action == "sample")
{
    var subjects = domain.GetData("gb.subjects") as Plunderspell.Guards.Guard[];
    if (subjects == null) return "no subjects";
    float t = UnityEngine.Time.time - (float)domain.GetData("gb.start");
    var lines = new System.Collections.Generic.List<string>();
    foreach (var g in subjects)
    {
        if (g == null) { lines.Add(t.ToString("F1") + " gone"); continue; }
        var seen = g.Sight.Visible;
        var dest = g.Navigator.Destination;
        lines.Add(t.ToString("F1") + " " + g.name + " " + g.CurrentState.GetType().Name
            + " pos " + g.transform.position.ToString("F1") + " yaw " + g.transform.eulerAngles.y.ToString("F0")
            + " sees " + (seen == null ? "-" : seen == player.transform ? "host" : "client")
            + " dest " + (dest.HasValue ? dest.Value.ToString("F1") : "-")
            + " strikes " + g.AttackSignal.Count
            + " toPlayer " + UnityEngine.Vector3.Distance(g.transform.position, player.transform.position).ToString("F1"));
    }
    return string.Join(" | ", lines);
}

return "unknown action " + action;

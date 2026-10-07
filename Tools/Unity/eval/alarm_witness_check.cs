// Play mode only, in a raid (#259). ACTION "spot": stands the local player 6 m in front of the living guard nearest
// the ground-floor centre that has another guard within 25 m, facing it, so exactly one guard sees them first.
// ACTION "sample": one line: alarm state, level, witnesses, chasers, and how many guards are patrolling,
// investigating, chasing or otherwise. Run "sample" every second or so after "spot".
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
        bool hasNeighbour = false;
        foreach (var o in guards) if (o != g && (o.transform.position - at).magnitude < 25f) hasNeighbour = true;
        if (!hasNeighbour) continue;
        float d = new UnityEngine.Vector2(at.x, at.z).magnitude;
        if (d < best) { best = d; pick = g; }
    }
    if (pick == null) return "no ground guard with a neighbour";
    var p = StateMachine.PlayerStateMachine.Local;
    var stand = pick.transform.position + pick.transform.forward * 6f + UnityEngine.Vector3.up * 0.2f;
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    rb.position = stand; p.transform.position = stand; rb.linearVelocity = UnityEngine.Vector3.zero;
    var look = pick.transform.position - stand;
    p.FaceYaw(UnityEngine.Mathf.Atan2(look.x, look.z) * UnityEngine.Mathf.Rad2Deg);
    UnityEngine.Physics.SyncTransforms();
    int near = 0;
    foreach (var o in guards) if (o != pick && (o.transform.position - pick.transform.position).magnitude < 25f) near++;
    return "spotter " + pick.name + " at " + pick.transform.position.ToString("F1") + ", " + near + " other guards within 25 m";
}
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
return director.State + " level " + director.AlarmLevel.ToString("F0") + " witnesses " + director.Alarm.Witnesses
    + " chasers " + director.ChasingGuards + " | guards patrol " + patrol + " investigate " + investigate + " chase " + chase + " other " + other;

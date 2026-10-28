// Play mode, either side: something done to this machine's own player, for guard_baseline_check.sh.
//   untouchable   turns the gym's no-damage on for this player, leaving it on if it already is (F8 is a toggle)
//   park x,y,z    holds this player still (kinematic) at that point, out of the way of a test
string action = "__ACTION__";
string arg = "__ARG__";
var p = StateMachine.PlayerStateMachine.Local;
if (p == null) return "no local player";

if (action == "untouchable")
{
    var gym = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Playtest.OphanGym>();
    if (gym == null) return "no gym";
    if (!gym.IsLocalPlayerUntouchable) gym.ToggleLocalPlayerUntouchable();
    return "untouchable " + gym.IsLocalPlayerUntouchable;
}

if (action == "park")
{
    string[] xyz = arg.Split(',');
    var c = System.Globalization.CultureInfo.InvariantCulture;
    var at = new UnityEngine.Vector3(float.Parse(xyz[0], c), float.Parse(xyz[1], c), float.Parse(xyz[2], c));
    var rb = p.GetComponent<UnityEngine.Rigidbody>();
    rb.isKinematic = true;
    rb.position = at; p.transform.position = at;
    UnityEngine.Physics.SyncTransforms();
    return "parked at " + at;
}

return "unknown action " + action;

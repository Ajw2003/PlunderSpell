// Puts the local player 2.5 m from the nearest closed door, facing it, for a Porta test.
var d = System.AppDomain.CurrentDomain;
d.SetData("track", false);
var p = StateMachine.PlayerStateMachine.Local;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var stats = Plunderspell.Core.GameServices.PlayerStats;
stats.GetType().GetProperty("Mana").SetValue(stats, stats.MaxMana);
Plunderspell.Castle.CastleDoor best = null; float bestD = float.MaxValue; int closed = 0, all = 0;
foreach (var door in UnityEngine.Object.FindObjectsByType<Plunderspell.Castle.CastleDoor>(FindObjectsSortMode.None))
{
    all++;
    if (door.IsOpen) continue;
    closed++;
    float dist = Vector3.Distance(door.transform.position, p.transform.position);
    if (dist < bestD) { bestD = dist; best = door; }
}
if (best == null) return "doors " + all + ", none closed";
Vector3 c = best.GetComponentInChildren<Collider>() != null ? best.GetComponentInChildren<Collider>().bounds.center : best.transform.position;
Vector3 n = best.transform.forward;
Vector3 stand = new Vector3(c.x, best.transform.position.y + 0.3f, c.z) + n * 2.5f;
p._rb.position = stand; p.transform.position = stand; p._rb.linearVelocity = Vector3.zero;
Vector3 to = c - p.CameraTransform.position;
p.FaceYaw(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg);
typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", flags).SetValue(p, -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg);
p.Look(Vector2.zero);
d.SetData("door", best);
return "doors " + all + ", closed " + closed + "; chose " + best.name + " (open " + best.IsOpen + ")";

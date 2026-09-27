// Puts the local player 5 m from the nearest calm, living guard, aimed at its chest, with full
// mana and health, and records the guard's state for read.cs. Run in Play mode during a raid.
var d = System.AppDomain.CurrentDomain;
var p = StateMachine.PlayerStateMachine.Local;
var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
var stats = Plunderspell.Core.GameServices.PlayerStats;
stats.GetType().GetProperty("Mana").SetValue(stats, stats.MaxMana);
typeof(StateMachine.PlayerStateMachine).GetField("_health", flags).SetValue(p, 100f);
Plunderspell.Guards.CastleGuard best = null; float bestD = float.MaxValue;
foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(FindObjectsSortMode.None))
{
    if (g.IsDead) continue;
    float dist = Vector3.Distance(g.transform.position, p.transform.position);
    if (dist < bestD) { bestD = dist; best = g; }
}
if (best == null) return "no guard";
Vector3 gp = best.transform.position;
Vector3 away = Vector3.ProjectOnPlane(p.transform.position - gp, Vector3.up);
if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
Vector3 stand = gp + away.normalized * 5f + Vector3.up * 0.3f;
p._rb.position = stand; p.transform.position = stand; p._rb.linearVelocity = Vector3.zero;
Vector3 to = (gp + Vector3.up * 1.1f) - p.CameraTransform.position;
float yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
float pitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
p.FaceYaw(yaw);
typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", flags).SetValue(p, pitch);
p.Look(Vector2.zero);
d.SetData("guard", best);
// Keep the crosshair on the guard's chest every frame, as a player tracking it would, so the
// 1.5 s keyed chant is not what decides a hit.
if (d.GetData("tracking") == null)
{
    d.SetData("tracking", true);
    Application.onBeforeRender += () =>
    {
        var gg = d.GetData("guard") as Plunderspell.Guards.CastleGuard;
        var pp = StateMachine.PlayerStateMachine.Local;
        if (gg == null || pp == null || d.GetData("track") == null || !(bool)d.GetData("track")) return;
        Vector3 t = (gg.transform.position + Vector3.up * 1.1f) - pp.CameraTransform.position;
        pp.FaceYaw(Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg);
        typeof(StateMachine.PlayerStateMachine).GetField("_xRotation", flags).SetValue(pp,
            -Mathf.Atan2(t.y, new Vector2(t.x, t.z).magnitude) * Mathf.Rad2Deg);
        pp.Look(Vector2.zero);
    };
}
d.SetData("track", true);
d.SetData("g0pos", gp);
d.SetData("g0hp", best.CurrentHealth);
d.SetData("gold0", stats.Gold);
return best.name + " hp " + best.CurrentHealth + " state " + best.State + " at " + bestD.ToString("F1") + " m, pitch " + pitch.ToString("F1");

// Play mode only, after set_out_seed.cs. The arrival point, and each guard's floor (by height) and the floor
// found under it by a downward ray, so a guard sunk into a slab or hanging in the air shows up (#255).
var d = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.RaidDirector>();
var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Raid.GuardSpawner>();
var sb = new System.Text.StringBuilder("arrival " + d.ArrivalPoint.ToString("F2") + "\n");
int keep = 0, ground = 0, crypt = 0, off = 0;
foreach (var g in spawner.Spawned)
{
    if (g == null) continue;
    var at = g.transform.position;
    if (at.y > 3f) keep++; else if (at.y < -1f) crypt++; else ground++;
    bool hit = UnityEngine.Physics.Raycast(at + UnityEngine.Vector3.up * 0.5f, UnityEngine.Vector3.down, out var h, 3f,
        UnityEngine.Physics.DefaultRaycastLayers, UnityEngine.QueryTriggerInteraction.Ignore);
    float gap = hit ? at.y - h.point.y : 99f;
    if (gap < -0.05f || gap > 0.3f) { off++; sb.Append("off-floor " + g.name + " at " + at.ToString("F2") + " gap " + gap.ToString("F2") + "\n"); }
}
sb.Append("guards keep " + keep + " ground " + ground + " crypt " + crypt + " off-floor " + off);
return sb.ToString();

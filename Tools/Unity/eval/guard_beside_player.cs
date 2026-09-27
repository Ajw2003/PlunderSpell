// Play mode only. Moves the nearest living guard to stand 1.5 m beside the local player, so a slam
// or a swing can be checked against it, and reports its health. Run again to read the health.
var caster = Plunderspell.Spells.SpellCastingSystem.Local;
if (caster == null) return "no local caster";
UnityEngine.Transform body = caster.transform.root;
Plunderspell.Guards.CastleGuard nearest = null;
float best = float.MaxValue;
foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.CastleGuard>(UnityEngine.FindObjectsSortMode.None))
{
    if (g == null || g.CurrentHealth <= 0f) continue;
    float d = (g.transform.position - body.position).sqrMagnitude;
    if (d < best) { best = d; nearest = g; }
}
if (nearest == null) return "no living guard";
if (UnityEngine.Mathf.Sqrt(best) > 2.5f)
{
    var agent = nearest.GetComponent<UnityEngine.AI.NavMeshAgent>();
    UnityEngine.Vector3 spot = body.position + new UnityEngine.Vector3(1.5f, -1f, 0f);
    if (agent != null && agent.enabled) agent.Warp(spot); else nearest.transform.position = spot;
}
return nearest.name + " at " + UnityEngine.Vector3.Distance(nearest.transform.position, body.position).ToString("F1") +
       " m, health " + nearest.CurrentHealth.ToString("F1");

// Play mode only. Moves the nearest living guard to stand 1.5 m beside the local player, so a slam
// or a swing can be checked against it, and reports its health. Run again to read the health.
var caster = Plunderspell.Spells.SpellCastingSystem.Local;
if (caster == null) return "no local caster";
UnityEngine.Transform body = caster.transform.root;
Plunderspell.Guards.Guard nearest = null;
float best = float.MaxValue;
foreach (var g in UnityEngine.Object.FindObjectsByType<Plunderspell.Guards.Guard>(UnityEngine.FindObjectsSortMode.None))
{
    if (g == null || g.CurrentHealth <= 0f) continue;
    float d = (g.transform.position - body.position).sqrMagnitude;
    if (d < best) { best = d; nearest = g; }
}
if (nearest == null) return "no living guard";
if (UnityEngine.Mathf.Sqrt(best) > 2.5f)
{
    // The fresh guard has no agent to warp: the director's navigation service moves it from its transform.
    nearest.transform.position = body.position + new UnityEngine.Vector3(1.5f, -1f, 0f);
}
return nearest.name + " at " + UnityEngine.Vector3.Distance(nearest.transform.position, body.position).ToString("F1") +
       " m, health " + nearest.CurrentHealth.ToString("F1");

// Play mode, raiding. Puts the first PIECES loose loot pieces on the extraction pad, through their rigidbodies,
// for the haul-comes-home check (#310). Then call the director's CallExtraction() after a physics step.
// Run: bash Tools/Unity/eval.sh --file Tools/Unity/eval/haul_to_pad.cs   (edit PIECES first)
const int PIECES = 2;
var zone = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Extraction.ExtractionZone>();
var lair = UnityEngine.GameObject.Find("LairRoom").transform;
int moved = 0; string names = "";
foreach (var v in UnityEngine.Object.FindObjectsByType<Plunderspell.Loot.LootValue>(UnityEngine.FindObjectsSortMode.None))
{
    if (moved == PIECES) break;
    if (v.transform.IsChildOf(lair) || (v.transform.position - lair.position).sqrMagnitude < 400f || v.IsRuined) continue;
    var rb = v.GetComponent<UnityEngine.Rigidbody>();
    var p = zone.transform.position + new UnityEngine.Vector3(0.6f * moved - 0.3f, 0.6f, 0.4f);
    if (rb != null) { rb.isKinematic = false; rb.position = p; rb.linearVelocity = UnityEngine.Vector3.zero; }
    v.transform.position = p;
    names += v.name + " "; moved++;
}
return "moved " + moved + ": " + names + "to pad at " + zone.transform.position;

// Play mode, host. Times a whole charged-beam hit as the Ophan deals it (HitAlongLine from the sky through a room piece,
// every part under it), on room pieces that have not been hit yet. Breaks the pieces it measures.
var pieces = new System.Collections.Generic.List<UnityEngine.Transform>();
foreach (var p in UnityEngine.Object.FindObjectsByType<Plunderspell.Fracture.FracturableObject>(UnityEngine.FindObjectsSortMode.None))
    if (!pieces.Contains(p.transform.parent)) pieces.Add(p.transform.parent);
var sb = new System.Text.StringBuilder();
var watch = new System.Diagnostics.Stopwatch();
int done = 0;
foreach (var piece in pieces)
{
    if (done >= 5) break;
    if (piece.name.StartsWith("DoorPlug")) continue;
    var parts = piece.GetComponentsInChildren<Plunderspell.Fracture.FracturableObject>();
    var floor = piece.Find("Part_Floor");
    if (floor == null) continue;
    var r = floor.GetComponent<UnityEngine.Renderer>();
    UnityEngine.Vector3 target = r.bounds.center + UnityEngine.Vector3.up * 1.2f;
    UnityEngine.Vector3 eye = target + new UnityEngine.Vector3(780f, 450f, 0f);
    UnityEngine.Vector3 cutEnd = target - (target - eye).normalized * 2f;
    watch.Restart();
    foreach (var part in parts) part.HitAlongLine(eye, cutEnd, 1.5f, 12f);
    sb.Append($"| {piece.name} ({parts.Length} parts): {watch.Elapsed.TotalMilliseconds:F0} ms ");
    done++;
}
return sb.ToString();

// Play mode, host. Times what one charged-beam break costs, stage by stage, on the room pieces nearest the castle's
// middle (aj saw freezes when walls broke, 2026-10-28): the first hit's effect setup, marking broken triangles, the
// collider re-cook, and the render mesh rebuild with caps. Breaks the pieces it measures.
const System.Reflection.BindingFlags All = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic
    | System.Reflection.BindingFlags.Public;
var type = typeof(Plunderspell.Fracture.FracturableObject);
var mark = type.GetMethod("MarkBroken", All);
var rebuild = type.GetMethod("RebuildCollisionMesh", All);
var effects = type.GetField("activeEffects", All);
var collider = type.GetField("meshCollider", All);
var mesh = type.GetField("collisionMesh", All);

var parts = UnityEngine.Object.FindObjectsByType<Plunderspell.Fracture.FracturableObject>(UnityEngine.FindObjectsSortMode.None);
UnityEngine.Vector3 middle = UnityEngine.Vector3.zero;
foreach (var p in parts) middle += p.transform.position;
middle /= UnityEngine.Mathf.Max(1, parts.Length);
System.Array.Sort(parts, (a, b) => (a.transform.position - middle).sqrMagnitude.CompareTo((b.transform.position - middle).sqrMagnitude));

var sb = new System.Text.StringBuilder();
sb.Append($"{parts.Length} parts. ");
var watch = new System.Diagnostics.Stopwatch();
int done = 0;
foreach (var part in parts)
{
    if (done >= 6) break;
    if (!part.TryGetComponent(out UnityEngine.MeshRenderer renderer) || !part.TryGetComponent(out UnityEngine.MeshFilter filter)) continue;
    if (part.name == "Part_Props") continue;
    int tris = filter.sharedMesh.triangles.Length / 3;
    UnityEngine.Vector3 at = renderer.bounds.center;

    watch.Restart();
    if (effects.GetValue(part) == null) effects.SetValue(part, Plunderspell.Fracture.FractureEngine.Fracture(part));
    double setup = watch.Elapsed.TotalMilliseconds;

    watch.Restart();
    for (int i = 0; i < 6; i++) mark.Invoke(part, new object[] { at + UnityEngine.Vector3.up * (i * 0.75f - 2f), 1.5f });
    double marking = watch.Elapsed.TotalMilliseconds;

    watch.Restart();
    rebuild.Invoke(part, null);
    double rebuildAll = watch.Elapsed.TotalMilliseconds;

    var mc = (UnityEngine.MeshCollider)collider.GetValue(part);
    var m = (UnityEngine.Mesh)mesh.GetValue(part);
    watch.Restart();
    if (mc != null && m != null) { mc.sharedMesh = null; mc.sharedMesh = m; }
    double cook = watch.Elapsed.TotalMilliseconds;

    sb.Append($"| {part.transform.parent.name}/{part.name} {tris} tris: setup {setup:F0} ms, mark x6 {marking:F0} ms, rebuild {rebuildAll:F0} ms (re-cook alone {cook:F0} ms) ");
    done++;
}
return sb.ToString();

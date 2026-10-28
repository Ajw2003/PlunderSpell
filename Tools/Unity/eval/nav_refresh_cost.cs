// Play mode, host. Times the guards' map refresh after a break (CastleNavGraph.RefreshAfterBreak) on lines from the sky
// through five room pieces, as the Ophan's charged beam calls it.
var generator = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Castle.ProceduralCastleGenerator>();
var graph = generator.LastGenerated.NavGraph;
var sb = new System.Text.StringBuilder();
var watch = new System.Diagnostics.Stopwatch();
int done = 0;
foreach (var room in UnityEngine.Object.FindObjectsByType<Plunderspell.Castle.CastleRoomModule>(UnityEngine.FindObjectsSortMode.InstanceID))
{
    if (done >= 5) break;
    UnityEngine.Vector3 target = room.transform.position + UnityEngine.Vector3.up * 1.2f;
    UnityEngine.Vector3 eye = target + new UnityEngine.Vector3(780f, 450f, 0f);
    watch.Restart();
    graph.RefreshAfterBreak(eye, target - (target - eye).normalized * 2f, 2f);
    sb.Append($"| {room.name}: {watch.Elapsed.TotalMilliseconds:F0} ms ");
    done++;
}
return sb.ToString();

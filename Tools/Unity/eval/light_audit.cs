// Play mode only, in a raid (#257). Every spawned fire, checked against what should hold it up:
//   a sconce needs a wall within 0.8 m behind its flame (it faces away from the wall);
//   any other fire needs solid geometry within 0.4 m under its own holder's base (the "Prop" child), or under
//   the flame when the room models the iron itself (the holder was dropped).
// Also counts fires and Light components per floor. Prints a summary line, then every suspect fire.
var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Atmosphere.CastleFireSpawner>();
if (spawner == null) return "no CastleFireSpawner";
// Crypt rooms end at the ground slab's underside (0.0); the keep floor's slab starts at 4.30.
string Floor(float y) => y > 4.3f ? "keep" : y < 0f ? "crypt" : "ground";
var mask = UnityEngine.Physics.DefaultRaycastLayers;
var ignore = UnityEngine.QueryTriggerInteraction.Ignore;
var perFloor = new System.Collections.Generic.Dictionary<string, int> { { "keep", 0 }, { "ground", 0 }, { "crypt", 0 } };
var sb = new System.Text.StringBuilder();
int noWall = 0, floating = 0;
foreach (var fire in spawner.Spawned)
{
    if (fire == null) continue;
    var light = fire.GetComponentInChildren<UnityEngine.Light>(true);
    var flame = light != null ? light.transform.position : fire.transform.position + UnityEngine.Vector3.up * 0.3f;
    perFloor[Floor(flame.y)]++;
    if (fire.name.StartsWith("Fire_Sconce"))
    {
        var back = -fire.transform.forward;
        if (!UnityEngine.Physics.Raycast(flame, back, 0.8f, mask, ignore) && !UnityEngine.Physics.Raycast(flame, -back, 0.8f, mask, ignore))
        {
            noWall++;
            sb.Append("no wall  " + fire.name + " at " + flame.ToString("F2") + "\n");
        }
        continue;
    }
    var prop = fire.transform.Find("Prop");
    float baseY = flame.y;
    if (prop != null)
        foreach (var r in prop.GetComponentsInChildren<UnityEngine.Renderer>()) baseY = UnityEngine.Mathf.Min(baseY, r.bounds.min.y);
    var from = new UnityEngine.Vector3(flame.x, baseY + 0.1f, flame.z);
    bool hit = UnityEngine.Physics.Raycast(from, UnityEngine.Vector3.down, out var h, 5f, mask, ignore);
    float gap = hit ? from.y - 0.1f - h.point.y : 99f;
    // A room-held brazier's flame sits about 0.42 m above its bowl, so a dropped holder (the room's iron) gets 0.6 m.
    if (gap > (prop != null ? 0.4f : 0.6f))
    {
        floating++;
        sb.Append("floats " + gap.ToString("F2") + " m  " + fire.name + (prop != null ? " (own holder)" : " (room's iron)") + " at " + flame.ToString("F2") + "\n");
    }
}
var lights = new System.Collections.Generic.Dictionary<string, int> { { "keep", 0 }, { "ground", 0 }, { "crypt", 0 } };
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
    if (l.type != UnityEngine.LightType.Directional) lights[Floor(l.transform.position.y)]++;
sb.Insert(0, "fires keep " + perFloor["keep"] + " ground " + perFloor["ground"] + " crypt " + perFloor["crypt"]
    + " | lights keep " + lights["keep"] + " ground " + lights["ground"] + " crypt " + lights["crypt"]
    + " | sconces with no wall " + noWall + " | floating " + floating + "\n");
return sb.ToString().TrimEnd();

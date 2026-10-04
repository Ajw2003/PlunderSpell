// Play mode only, in a raid (#257). Every spawned fire, measured at its light (the flame; the fire's own root sits
// in the floor slab): which floor it is on, whether solid floor lies within 4 m below the flame (a curtain-wall torch hangs 3.5 m over the ground outside) (else it floats),
// and whether the flame is buried in solid geometry other than the fire's own. Prints a summary and every
// suspect fire. Also counts Light components per floor.
var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Atmosphere.CastleFireSpawner>();
if (spawner == null) return "no CastleFireSpawner";
// Crypt rooms end at the ground slab's underside (0.0); the keep floor's slab starts at 4.30.
string Floor(float y) => y > 4.3f ? "keep" : y < 0f ? "crypt" : "ground";
var perFloor = new System.Collections.Generic.Dictionary<string, int> { { "keep", 0 }, { "ground", 0 }, { "crypt", 0 } };
var sb = new System.Text.StringBuilder();
int floating = 0, buried = 0;
foreach (var fire in spawner.Spawned)
{
    if (fire == null) continue;
    var light = fire.GetComponentInChildren<UnityEngine.Light>(true);
    var at = light != null ? light.transform.position : fire.transform.position + UnityEngine.Vector3.up * 0.3f;
    perFloor[Floor(at.y)]++;
    bool hasFloor = UnityEngine.Physics.Raycast(at, UnityEngine.Vector3.down, out var hit, 4f,
        UnityEngine.Physics.DefaultRaycastLayers, UnityEngine.QueryTriggerInteraction.Ignore);
    bool isBuried = false;
    foreach (var c in UnityEngine.Physics.OverlapSphere(at, 0.05f,
        UnityEngine.Physics.DefaultRaycastLayers, UnityEngine.QueryTriggerInteraction.Ignore))
        if (!c.transform.IsChildOf(fire.transform)) isBuried = true;
    if (!hasFloor) { floating++; sb.Append("floats " + fire.name + " at " + at.ToString("F2") + "\n"); }
    if (isBuried) { buried++; sb.Append("buried " + fire.name + " at " + at.ToString("F2") + "\n"); }
}
var lights = new System.Collections.Generic.Dictionary<string, int> { { "keep", 0 }, { "ground", 0 }, { "crypt", 0 } };
foreach (var l in UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsSortMode.None))
    if (l.type != UnityEngine.LightType.Directional) lights[Floor(l.transform.position.y)]++;
sb.Insert(0, "fires keep " + perFloor["keep"] + " ground " + perFloor["ground"] + " crypt " + perFloor["crypt"]
    + " | lights keep " + lights["keep"] + " ground " + lights["ground"] + " crypt " + lights["crypt"]
    + " | floating " + floating + " buried " + buried + "\n");
return sb.ToString().TrimEnd();

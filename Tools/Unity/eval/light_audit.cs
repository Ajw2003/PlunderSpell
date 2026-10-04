// Play mode only, in a raid (#257). Every spawned fire: which floor it is on, whether solid floor lies within 3 m
// below it (else it floats), and whether its flame point is buried in solid geometry. Prints a summary and every
// suspect fire. Also counts Light components per floor.
var spawner = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Atmosphere.CastleFireSpawner>();
if (spawner == null) return "no CastleFireSpawner";
string Floor(float y) => y > 4.4f ? "keep" : y < -0.5f ? "crypt" : "ground";
var perFloor = new System.Collections.Generic.Dictionary<string, int> { { "keep", 0 }, { "ground", 0 }, { "crypt", 0 } };
var sb = new System.Text.StringBuilder();
int floating = 0, buried = 0;
foreach (var fire in spawner.Spawned)
{
    if (fire == null) continue;
    var at = fire.transform.position;
    perFloor[Floor(at.y)]++;
    bool hasFloor = UnityEngine.Physics.Raycast(at + UnityEngine.Vector3.up * 0.05f, UnityEngine.Vector3.down, out var hit, 3f,
        UnityEngine.Physics.DefaultRaycastLayers, UnityEngine.QueryTriggerInteraction.Ignore);
    bool isBuried = false;
    foreach (var c in UnityEngine.Physics.OverlapSphere(at + UnityEngine.Vector3.up * 0.3f, 0.05f,
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

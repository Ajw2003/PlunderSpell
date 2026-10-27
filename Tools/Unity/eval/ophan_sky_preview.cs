// Play mode, Editor only: a preview of the Ophan of Hours far off in the sky, for the #418 build plan page. Not game code.
// __ARG__ is "scale,distance,elevation": times its modelled size, metres from the castle's centre to the Ophan's centre
// along the ground, and the angle above the horizon its centre sits at, seen from the castle's centre. It faces the
// castle, eye first. __ACTION__ is the output folder (absolute, forward slashes). Views come from the player's own
// camera settings (60-degree view, its far clip plane), from three places with a clear line to the Ophan: inside a room,
// a courtyard, and the curtain wall's walk on the far side.
string outDir = "__ACTION__";
var args = "__ARG__".Split(',');
var ci = System.Globalization.CultureInfo.InvariantCulture;
float scale = float.Parse(args[0], ci), distance = float.Parse(args[1], ci), elevation = float.Parse(args[2], ci);
var report = new System.Text.StringBuilder();

var gen = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Castle.ProceduralCastleGenerator>();
if (gen == null) return "no castle";
var castle = new UnityEngine.Bounds(gen.transform.position, UnityEngine.Vector3.zero);
foreach (var r in gen.GetComponentsInChildren<UnityEngine.Renderer>()) castle.Encapsulate(r.bounds);
var centre = new UnityEngine.Vector3(castle.center.x, 0f, castle.center.z);
var dir = UnityEngine.Vector3.right;

// What the player's camera and the scene's fog would do to something this far away.
var playerCam = UnityEngine.Camera.main;
float farClip = playerCam != null ? playerCam.farClipPlane : -1f;
report.Append("player far clip " + farClip + " m; fog " + UnityEngine.RenderSettings.fog + " " + UnityEngine.RenderSettings.fogMode
    + " density " + UnityEngine.RenderSettings.fogDensity + " start " + UnityEngine.RenderSettings.fogStartDistance
    + " end " + UnityEngine.RenderSettings.fogEndDistance + " | ");

var old = UnityEngine.GameObject.Find("OphanScalePreview");
if (old != null) UnityEngine.Object.DestroyImmediate(old);   // Destroy waits for the frame end; the old one would render
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
    "Assets/Models/ArtBible/Enemies/Fear/TheOphanOfHours/TheOphanOfHours.fbx");
if (model == null) return "no Ophan model";
var ophan = UnityEngine.Object.Instantiate(model);
ophan.name = "OphanScalePreview";
ophan.transform.localScale *= scale;
var mid = centre + dir * distance + UnityEngine.Vector3.up * (distance * UnityEngine.Mathf.Tan(elevation * UnityEngine.Mathf.Deg2Rad));
ophan.transform.rotation = UnityEngine.Quaternion.LookRotation((centre + UnityEngine.Vector3.up * 1.65f) - mid);
var b = new UnityEngine.Bounds(ophan.transform.position, UnityEngine.Vector3.zero);
foreach (var r in ophan.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
ophan.transform.position += mid - b.center;
b.center = mid;
float height = 9.6f * scale, width = 11f * scale;
report.Append("Ophan " + height.ToString("F0") + " m tall, " + width.ToString("F0") + " m across, centre " + distance + " m out and "
    + mid.y.ToString("F0") + " m up; lowest point " + (mid.y - height / 2f).ToString("F0") + " m | ");

// Viewpoints: scan the castle's 12 m cells for a floor with a clear line to the Ophan, and sort by how walled in it is.
UnityEngine.Vector3? room = null, yard = null;
for (float x = castle.min.x + 6f; x < castle.max.x && (room == null || yard == null); x += 12f)
for (float z = castle.min.z + 6f; z < castle.max.z && (room == null || yard == null); z += 12f)
{
    // 3 m off the cell's centre: rooms have an archway in the middle of every wall, and a ray from the centre leaves through it.
    if (!UnityEngine.Physics.Raycast(new UnityEngine.Vector3(x + 3f, 3f, z + 3f), UnityEngine.Vector3.down, out var floor, 6f)) continue;
    var eye = floor.point + UnityEngine.Vector3.up * 1.65f;
    if (UnityEngine.Physics.Raycast(eye, UnityEngine.Vector3.up, 30f)) continue;                        // something overhead
    if (UnityEngine.Physics.Linecast(eye, mid)) continue;                                                // no clear line
    int walls = 0;
    foreach (var d in new[] { UnityEngine.Vector3.forward, UnityEngine.Vector3.back, UnityEngine.Vector3.left, UnityEngine.Vector3.right })
        if (UnityEngine.Physics.Raycast(eye, d, 7f)) walls++;
    if (walls >= 2 && room == null) room = eye;
    if (walls == 0 && yard == null) yard = eye;
}
UnityEngine.Vector3? walk = null;
var walkTop = centre - dir * (castle.extents.x - 3f) + UnityEngine.Vector3.up * 30f;
if (UnityEngine.Physics.Raycast(walkTop, UnityEngine.Vector3.down, out var wallHit, 40f)) walk = wallHit.point + UnityEngine.Vector3.up * 1.65f;

var camGo = new UnityEngine.GameObject("OphanScaleCamera");
var cam = camGo.AddComponent<UnityEngine.Camera>();
cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f;
var rt = new UnityEngine.RenderTexture(1600, 900, 24);
cam.targetTexture = rt;

void Shot(string name, UnityEngine.Vector3? at, float clip)
{
    if (at == null) { report.Append(name + ": no viewpoint found | "); return; }
    var eye = at.Value;
    cam.farClipPlane = clip;
    cam.transform.position = eye;
    cam.transform.rotation = UnityEngine.Quaternion.LookRotation(mid - eye);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0);
    tex.Apply();
    System.IO.File.WriteAllBytes(outDir + "/" + name + "-" + scale + "x-" + distance + "m.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.RenderTexture.active = null;
    float dist = UnityEngine.Vector3.Distance(eye, mid);
    float tall = 2f * UnityEngine.Mathf.Atan(height / 2f / dist) * UnityEngine.Mathf.Rad2Deg;
    float wide = 2f * UnityEngine.Mathf.Atan(width / 2f / dist) * UnityEngine.Mathf.Rad2Deg;
    report.Append(name + " at " + eye.ToString("F0") + ", " + dist.ToString("F0") + " m away: " + tall.ToString("F0") + " deg tall ("
        + (tall / 60f * 100f).ToString("F0") + "% of the view's height), " + wide.ToString("F0") + " deg wide ("
        + (wide / 91.5f * 100f).ToString("F0") + "% of its width) | ");
}

float clipNeeded = distance * 1.6f;
Shot("room", room, UnityEngine.Mathf.Max(farClip, clipNeeded));
Shot("courtyard", yard, UnityEngine.Mathf.Max(farClip, clipNeeded));
Shot("wallwalk", walk, UnityEngine.Mathf.Max(farClip, clipNeeded));
// The same courtyard view with the player's camera's own far clip, to show whether it needs raising.
if (farClip > 0f) Shot("courtyard-playerclip", yard, farClip);
UnityEngine.Object.Destroy(camGo);
rt.Release();
return report.ToString();

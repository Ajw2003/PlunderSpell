// Play mode, Editor only: a preview of the Ophan of Hours' size in the sky for the #418 build plan page. Not game code.
// Places the Ophan's model (not a prefab: none exists yet) outside the curtain wall. __ARG__ is "scale,bottom,beyond":
// times its modelled size, the height of its lowest point in metres, and how far its centre sits beyond the wall. Then
// saves views from a 60-degree camera (the player's) at the far wall and at the near wall, on the ground and on the
// wall top.
// __ACTION__ is the output folder (absolute, forward slashes).
string outDir = "__ACTION__";
var args = "__ARG__".Split(',');
float scale = float.Parse(args[0], System.Globalization.CultureInfo.InvariantCulture);
float bottom = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
float beyond = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);

var gen = UnityEngine.Object.FindFirstObjectByType<Plunderspell.Castle.ProceduralCastleGenerator>();
if (gen == null) return "no castle";
var castle = new UnityEngine.Bounds(gen.transform.position, UnityEngine.Vector3.zero);
foreach (var r in gen.GetComponentsInChildren<UnityEngine.Renderer>()) castle.Encapsulate(r.bounds);
var centre = new UnityEngine.Vector3(castle.center.x, 0f, castle.center.z);
float wall = castle.extents.x;                 // centre to the curtain wall's outer face, along x
var dir = UnityEngine.Vector3.right;

var old = UnityEngine.GameObject.Find("OphanScalePreview");
if (old != null) UnityEngine.Object.DestroyImmediate(old);   // Destroy waits for the frame end; the old one would render
var model = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(
    "Assets/Models/ArtBible/Enemies/Fear/TheOphanOfHours/TheOphanOfHours.fbx");
if (model == null) return "no Ophan model";
var ophan = UnityEngine.Object.Instantiate(model);
ophan.name = "OphanScalePreview";
ophan.transform.localScale *= scale;
ophan.transform.rotation = UnityEngine.Quaternion.LookRotation(-dir);   // faces the castle
var b = new UnityEngine.Bounds(ophan.transform.position, UnityEngine.Vector3.zero);
foreach (var r in ophan.GetComponentsInChildren<UnityEngine.Renderer>()) b.Encapsulate(r.bounds);
var target = centre + dir * (wall + beyond);
ophan.transform.position += new UnityEngine.Vector3(target.x - b.center.x, bottom - b.min.y, target.z - b.center.z);
b.center += new UnityEngine.Vector3(target.x - b.center.x, bottom - b.min.y, target.z - b.center.z);

var camGo = new UnityEngine.GameObject("OphanScaleCamera");
var cam = camGo.AddComponent<UnityEngine.Camera>();
cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 2000f;
var rt = new UnityEngine.RenderTexture(1600, 900, 24);
cam.targetTexture = rt;
var report = new System.Text.StringBuilder();
report.Append("castle centre " + centre.ToString("F1") + " wall " + wall.ToString("F1") + " m; Ophan " + b.size.y.ToString("F1")
    + " m tall, " + b.size.z.ToString("F1") + " m across, centre " + b.center.ToString("F1") + " | ");

void Shot(string name, UnityEngine.Vector3 eye, UnityEngine.Vector3 lookAt)
{
    cam.transform.position = eye;
    cam.transform.rotation = UnityEngine.Quaternion.LookRotation(lookAt - eye);
    cam.Render();
    UnityEngine.RenderTexture.active = rt;
    var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
    tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0);
    tex.Apply();
    System.IO.File.WriteAllBytes(outDir + "/" + name + "-" + scale + "x.png", UnityEngine.ImageConversion.EncodeToPNG(tex));
    UnityEngine.RenderTexture.active = null;
    float share = 0f;
    {
        // Share of the screen height the Ophan's bounds take, from its top and bottom corners projected.
        float lo = 1f, hi = 0f;
        foreach (float y in new[] { b.min.y, b.max.y })
        {
            var v = cam.WorldToViewportPoint(new UnityEngine.Vector3(b.center.x, y, b.center.z));
            if (v.z > 0f) { lo = UnityEngine.Mathf.Min(lo, UnityEngine.Mathf.Clamp01(v.y)); hi = UnityEngine.Mathf.Max(hi, UnityEngine.Mathf.Clamp01(v.y)); }
        }
        share = UnityEngine.Mathf.Max(0f, hi - lo);
    }
    report.Append(name + " from " + UnityEngine.Vector3.Distance(new UnityEngine.Vector3(eye.x, 0, eye.z), new UnityEngine.Vector3(b.center.x, 0, b.center.z)).ToString("F0")
        + " m: " + (share * 100f).ToString("F0") + "% of screen height | ");
}

var far = centre - dir * (wall - 6f);
var near = centre + dir * (wall - 6f);
var mid = b.center;
// Far: looking at its middle from across the castle, as in the reference.
Shot("far-ground", far + UnityEngine.Vector3.up * 1.65f, mid);
Shot("far-walltop", far + UnityEngine.Vector3.up * 7.15f, mid);
// Close: looking up at its middle, as in the reference.
Shot("close-ground", near + UnityEngine.Vector3.up * 1.65f, mid);
Shot("close-walltop", near + UnityEngine.Vector3.up * 7.15f, mid);
UnityEngine.Object.Destroy(camGo);
rt.Release();
return report.ToString();

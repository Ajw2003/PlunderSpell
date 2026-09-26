// Run in a live Editor with:  unity command eval_file --file <this file>
// Renders every loot item as it sits in a carrier's hand (LootPickup.AttachToSocket), with a red
// dot where the hand socket is, into loot-held.png beside this file. Read-only on the project.
const int Tile = 320;
string outDir = @"C:\Users\Ayden's mini\Desktop\GameDev\PlunderSpell-trialmerge\docs\generated\merge-audit-2026-09-24\";
var paths = new System.Collections.Generic.List<string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Loot" }))
    paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
paths.Sort(System.StringComparer.Ordinal);

int cols = 6, rows = Mathf.CeilToInt(paths.Count / (float)cols);
var tex = new Texture2D(cols * Tile, rows * Tile, TextureFormat.RGB24, false);
var pru = new UnityEditor.PreviewRenderUtility();
pru.camera.fieldOfView = 30f;
pru.camera.clearFlags = CameraClearFlags.SolidColor;
pru.camera.backgroundColor = new Color(0.22f, 0.22f, 0.25f);
pru.lights[0].intensity = 3.2f;
pru.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
pru.lights[1].intensity = 1.8f;
pru.ambientColor = new Color(0.9f, 0.9f, 0.95f);
var red = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
red.SetColor("_BaseColor", Color.red);
var legend = new System.Text.StringBuilder("| Row | Col | Item | Held by |\n|---|---|---|---|\n");
try
{
    for (int i = 0; i < paths.Count; i++)
    {
        var socket = new GameObject("HandSocket");
        pru.AddSingleGO(socket);
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        UnityEngine.Object.DestroyImmediate(dot.GetComponent<Collider>());
        dot.GetComponent<MeshRenderer>().sharedMaterial = red;
        pru.AddSingleGO(dot);

        var go = pru.InstantiatePrefabInScene(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(paths[i]));
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        var pickup = go.GetComponent<RogueAi.Loot.LootPickup>();
        pickup.CaptureUprightRotation();
        pickup.AttachToSocket(socket.transform);

        var b = new Bounds(); bool first = true;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>()) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        float radius = Mathf.Max(0.1f, b.extents.magnitude);
        dot.transform.localScale = Vector3.one * radius * 0.12f;
        dot.transform.position = socket.transform.position;
        float dist = radius / Mathf.Sin(15f * Mathf.Deg2Rad) * 1.05f;
        var dir = Quaternion.Euler(10f, 155f, 0f);
        pru.camera.transform.rotation = dir;
        pru.camera.transform.position = b.center - dir * Vector3.forward * dist;
        pru.camera.nearClipPlane = dist * 0.02f;
        pru.camera.farClipPlane = dist * 4f;

        pru.BeginPreview(new Rect(0, 0, Tile, Tile), GUIStyle.none);
        pru.Render(true);
        var rt = (RenderTexture)pru.EndPreview();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, Tile, Tile), (i % cols) * Tile, (rows - 1 - i / cols) * Tile);
        RenderTexture.active = prev;

        legend.AppendLine($"| {i / cols + 1} | {i % cols + 1} | {System.IO.Path.GetFileNameWithoutExtension(paths[i])} | {(pickup.GripPoint != null ? "grip point" : "mesh centre")} |");
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(dot);
        UnityEngine.Object.DestroyImmediate(socket);
    }
}
finally { pru.Cleanup(); UnityEngine.Object.DestroyImmediate(red); }
tex.Apply();
System.IO.File.WriteAllBytes(outDir + "loot-held.png", tex.EncodeToPNG());
System.IO.File.WriteAllText(outDir + "loot-held.legend.md", legend.ToString());
UnityEngine.Object.DestroyImmediate(tex);
return paths.Count + " items rendered";

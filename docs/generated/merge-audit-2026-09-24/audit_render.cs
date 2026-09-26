// Run in a live Editor with:  unity command eval_file --file <this file>
// Renders labelled contact sheets of the enemies, loot items and castle rooms the trial merge
// holds, into docs/generated/merge-audit-2026-09-24/. Read-only on the project: every object is
// instantiated into a PreviewRenderUtility scene and destroyed afterwards.
const int Tile = 320;
string outDir = @"C:\Users\Ayden's mini\Desktop\GameDev\PlunderSpell-trialmerge\docs\generated\merge-audit-2026-09-24\";
var log = new System.Text.StringBuilder();

System.Func<string, string[], System.Collections.Generic.List<string>> find = (filter, folders) =>
{
    var list = new System.Collections.Generic.List<string>();
    foreach (var g in UnityEditor.AssetDatabase.FindAssets(filter, folders))
        list.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    list.Sort(System.StringComparer.Ordinal);
    return list;
};

System.Action<string, System.Collections.Generic.List<(string path, string label, Color tint)>, bool> sheet = (fileName, items, isRoom) =>
{
    int cols = Mathf.Min(6, Mathf.Max(1, items.Count));
    int rows = Mathf.CeilToInt(items.Count / (float)cols);
    var tex = new Texture2D(cols * Tile, rows * Tile, TextureFormat.RGB24, false);
    var pru = new UnityEditor.PreviewRenderUtility();
    pru.camera.fieldOfView = 30f;
    pru.camera.clearFlags = CameraClearFlags.SolidColor;
    pru.lights[0].intensity = 3.2f;
    pru.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0f);
    pru.lights[1].intensity = 1.8f;
    pru.ambientColor = new Color(0.9f, 0.9f, 0.95f);
    for (int i = 0; i < items.Count; i++)
    {
        var (path, label, tint) = items[i];
        var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
        pru.camera.backgroundColor = tint;
        pru.BeginPreview(new Rect(0, 0, Tile, Tile), GUIStyle.none);
        GameObject go = null;
        if (asset != null)
        {
            go = pru.InstantiatePrefabInScene(asset);
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = new Bounds(go.transform.position, Vector3.zero);
            bool first = true;
            foreach (var r in rs)
            {
                if (r is ParticleSystemRenderer) continue;
                if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
            }
            float radius = Mathf.Max(0.1f, b.extents.magnitude);
            float dist = radius / Mathf.Sin(pru.camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            var dir = isRoom ? Quaternion.Euler(45f, 35f, 0f) : Quaternion.Euler(10f, 155f, 0f);
            pru.camera.transform.rotation = dir;
            pru.camera.transform.position = b.center - dir * Vector3.forward * dist;
            pru.camera.nearClipPlane = dist * 0.02f;
            pru.camera.farClipPlane = dist * 4f;
        }
        pru.Render(true);
        var rt = (RenderTexture)pru.EndPreview();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        int x = (i % cols) * Tile, y = (rows - 1 - i / cols) * Tile;
        tex.ReadPixels(new Rect(0, 0, Tile, Tile), x, y);
        RenderTexture.active = prev;
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        if (asset == null) log.AppendLine("MISSING " + path);
    }
    pru.Cleanup();
    tex.Apply();
    System.IO.File.WriteAllBytes(outDir + fileName + ".png", tex.EncodeToPNG());
    // The legend is the label per tile, in reading order: the image has no text of its own.
    var legend = new System.Text.StringBuilder();
    for (int i = 0; i < items.Count; i++)
        legend.AppendLine($"| {i / cols + 1} | {i % cols + 1} | {items[i].label} | `{items[i].path}` |");
    System.IO.File.WriteAllText(outDir + fileName + ".legend.md", "| Row | Col | What | Asset |\n|---|---|---|---|\n" + legend);
    log.AppendLine(fileName + ": " + items.Count + " tiles");
    UnityEngine.Object.DestroyImmediate(tex);
};

Color green = new Color(0.20f, 0.30f, 0.22f), amber = new Color(0.36f, 0.30f, 0.16f), grey = new Color(0.22f, 0.22f, 0.25f);

// Enemies: dreamy's AllEnemies prefabs, green when a roster spawns them, amber when nothing does.
var rostered = new System.Collections.Generic.HashSet<string>();
foreach (var p in find("t:EnemyRoster", new[] { "Assets/_Project/Data" }))
    foreach (var en in UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EnemyRoster>(p).Entries) rostered.Add(en.EnemyId);
var enemies = new System.Collections.Generic.List<(string, string, Color)>();
foreach (var p in find("t:Prefab", new[] { "Assets/Models/ArtBible/AllEnemies" }))
{
    var n = System.IO.Path.GetFileNameWithoutExtension(p);
    bool r = rostered.Contains(n);
    enemies.Add((p, n + (r ? " - in a raid roster" : " - NOT in any roster"), r ? green : amber));
}
sheet("enemies", enemies, false);

// Loot items per era, from era's forged prefabs.
var loot = new System.Collections.Generic.List<(string, string, Color)>();
foreach (var era in new[] { "BronzeAge", "HighMedieval", "LateMedieval", "AgeOfPowder" })
    foreach (var p in find("t:Prefab", new[] { "Assets/_Project/Prefabs/Loot/" + era }))
        loot.Add((p, era + " / " + System.IO.Path.GetFileNameWithoutExtension(p), grey));
sheet("loot-items", loot, false);

// Late Medieval rooms: green = prefab in the registry, amber = castle-bench model with no prefab yet.
var lateRooms = new System.Collections.Generic.List<(string, string, Color)>();
var latePrefabNames = new System.Collections.Generic.HashSet<string>();
foreach (var p in find("t:Prefab", new[] { "Assets/_Project/Prefabs/Castle/LateMedieval" }))
{
    var n = System.IO.Path.GetFileNameWithoutExtension(p);
    latePrefabNames.Add(n);
    lateRooms.Add((p, n + " - prefab (era branch)", green));
}
foreach (var p in find("t:Model", new[] { "Assets/_Project/Art/Models/Castle/LateMedieval" }))
{
    var n = System.IO.Path.GetFileNameWithoutExtension(p);
    if (!latePrefabNames.Contains(n)) lateRooms.Add((p, n + " - model only (castle-bench branch)", amber));
}
sheet("rooms-late-medieval", lateRooms, true);

var bronzeRooms = new System.Collections.Generic.List<(string, string, Color)>();
foreach (var p in find("t:Prefab", new[] { "Assets/_Project/Prefabs/Castle/BronzeAge" }))
    bronzeRooms.Add((p, System.IO.Path.GetFileNameWithoutExtension(p) + " - prefab (era branch)", green));
sheet("rooms-bronze-age", bronzeRooms, true);

return log.ToString();

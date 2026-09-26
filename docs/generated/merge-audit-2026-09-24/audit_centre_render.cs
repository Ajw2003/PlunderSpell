// Run in a live Editor with:  unity command eval_file --file <this file>
// Renders the room each era puts at the castle's centre cell (the CryptChamberFinal role), same
// camera and scale for all, into centre-rooms.png beside this file, with the loot anchor points
// marked in yellow. Read-only on the project.
const int Tile = 640;
string outDir = @"C:\Users\Ayden's mini\Desktop\GameDev\PlunderSpell-trialmerge\docs\generated\merge-audit-2026-09-24\";
var cat = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EraContentCatalogue>("Assets/_Project/Data/Eras/EraContentCatalogue.asset");
var defaultRooms = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Castle.CastleRoomRegistry>("Assets/_Project/Data/Castle/CastleRoomRegistry.asset");
var rows = new System.Collections.Generic.List<(string label, RogueAi.Castle.CastleRoomModuleData data)>();
foreach (var era in new[] { RogueAi.Inventory.HistoricalEra.BronzeAge, RogueAi.Inventory.HistoricalEra.HighMedieval, RogueAi.Inventory.HistoricalEra.LateMedieval })
{
    var reg = cat.For(era)?.Rooms ?? defaultRooms;
    rows.Add((era.ToString(), reg.GetById("CryptChamberFinal")));
}
var tex = new Texture2D(rows.Count * Tile, Tile, TextureFormat.RGB24, false);
var pru = new UnityEditor.PreviewRenderUtility();
pru.camera.fieldOfView = 30f;
pru.camera.clearFlags = CameraClearFlags.SolidColor;
pru.camera.backgroundColor = new Color(0.22f, 0.22f, 0.25f);
pru.lights[0].intensity = 3.2f;
pru.lights[0].transform.rotation = Quaternion.Euler(50f, 30f, 0f);
pru.lights[1].intensity = 1.8f;
pru.ambientColor = new Color(0.9f, 0.9f, 0.95f);
var yellow = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
yellow.SetColor("_BaseColor", Color.yellow);
var legend = new System.Text.StringBuilder();
try
{
    for (int i = 0; i < rows.Count; i++)
    {
        var d = rows[i].data;
        var go = pru.InstantiatePrefabInScene(d.Prefab);
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false;
        var markers = new System.Collections.Generic.List<GameObject>();
        foreach (var a in d.LootAnchors)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DestroyImmediate(m.GetComponent<Collider>());
            m.GetComponent<MeshRenderer>().sharedMaterial = yellow;
            pru.AddSingleGO(m);
            m.transform.position = go.transform.TransformPoint(a) + Vector3.up * 0.5f;
            m.transform.localScale = Vector3.one * 0.5f;
            markers.Add(m);
        }
        // Same framing for every era: a 12 m cell seen from above at 50 degrees.
        var dir = Quaternion.Euler(50f, 35f, 0f);
        pru.camera.transform.rotation = dir;
        pru.camera.transform.position = go.transform.position + Vector3.up * 1.5f - dir * Vector3.forward * 26f;
        pru.camera.nearClipPlane = 0.3f; pru.camera.farClipPlane = 100f;
        pru.BeginPreview(new Rect(0, 0, Tile, Tile), GUIStyle.none);
        pru.Render(true);
        var rt = (RenderTexture)pru.EndPreview();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, Tile, Tile), i * Tile, 0);
        RenderTexture.active = prev;
        legend.AppendLine($"{i + 1}. {rows[i].label}: {d.Prefab.name}, {d.LootAnchors.Length} loot anchors, all filled, the richest on the first");
        foreach (var m in markers) UnityEngine.Object.DestroyImmediate(m);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
finally { pru.Cleanup(); UnityEngine.Object.DestroyImmediate(yellow); }
tex.Apply();
System.IO.File.WriteAllBytes(outDir + "centre-rooms.png", tex.EncodeToPNG());
UnityEngine.Object.DestroyImmediate(tex);
return legend.ToString();

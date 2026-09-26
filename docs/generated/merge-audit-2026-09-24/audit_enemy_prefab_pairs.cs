// For each enemy that has both an era-forged gameplay prefab (Prefabs/Enemies/<Era>) and a
// hand-made AllEnemies prefab, compares height, scale and gameplay components side by side.
var sb = new System.Text.StringBuilder();
System.Func<GameObject, string> describe = go =>
{
    var b = new Bounds(); bool first = true;
    foreach (var r in go.GetComponentsInChildren<Renderer>(true)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
    var comps = new System.Collections.Generic.List<string>();
    foreach (var c in go.GetComponents<Component>()) if (c != null && !(c is Transform)) comps.Add(c.GetType().Name);
    return "h=" + b.size.y.ToString("0.00") + "m scale=" + go.transform.localScale.x.ToString("0.00") + " [" + string.Join(",", comps) + "]";
};
var forged = new System.Collections.Generic.Dictionary<string, string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Enemies" }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    forged[System.IO.Path.GetFileNameWithoutExtension(p)] = p;
}
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Models/ArtBible/AllEnemies" }))
{
    var p = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var n = System.IO.Path.GetFileNameWithoutExtension(p);
    sb.AppendLine(n);
    sb.AppendLine("  AllEnemies: " + describe(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p)));
    sb.AppendLine("  era forged: " + (forged.TryGetValue(n, out var fp) ? describe(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(fp)) + " " + fp : "none"));
}
return sb.ToString();

// Upright world bounds of every forged loot prefab (instantiated at the origin, as imported),
// used to place each item's grip point.
var sb = new System.Text.StringBuilder();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Loot" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path));
    try
    {
        var b = new Bounds(); bool first = true;
        foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds); }
        sb.AppendLine(System.IO.Path.GetFileNameWithoutExtension(path) + " size=" + b.size.ToString("F2") + " min=" + b.min.ToString("F2") + " rootRot=" + go.transform.rotation.eulerAngles.ToString("F0") + " scale=" + go.transform.localScale.ToString("F2"));
    }
    finally { UnityEngine.Object.DestroyImmediate(go); }
}
return sb.ToString();

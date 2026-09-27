// #134: prints, for each weapon and legacy loot prefab, its grip point (if any) and its mesh bounds
// in the prefab's own space, so grips can be placed on the handle rather than guessed.
var sb = new System.Text.StringBuilder();
var paths = new System.Collections.Generic.List<string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Weapons" })) paths.Add(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
foreach (var f in System.IO.Directory.GetFiles("Assets/_Project/Prefabs/Loot", "*.prefab")) paths.Add(f.Replace(System.IO.Path.DirectorySeparatorChar, '/'));
foreach (var path in paths)
{
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(path);
    var item = root.GetComponent<Item>();
    var gripField = typeof(Item).GetField("_gripPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var grip = item != null ? (Transform)gripField.GetValue(item) : null;
    var lp = root.GetComponent<Plunderspell.Loot.LootPickup>();
    Bounds b = new Bounds(); bool any = false;
    foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
    {
        if (mf.sharedMesh == null) continue;
        var mb = mf.sharedMesh.bounds;
        for (int i = 0; i < 8; i++)
        {
            var c = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            var w = root.transform.InverseTransformPoint(mf.transform.TransformPoint(c));
            if (!any) { b = new Bounds(w, Vector3.zero); any = true; } else b.Encapsulate(w);
        }
    }
    sb.Append(System.IO.Path.GetFileNameWithoutExtension(path) + ": bounds min " + b.min.ToString("F2") + " max " + b.max.ToString("F2")
        + " | item grip " + (grip != null ? grip.localPosition.ToString("F2") : "none")
        + " | pickup grip " + (lp != null && lp.GripPoint != null ? lp.GripPoint.localPosition.ToString("F2") : (lp != null ? "none" : "no pickup")) + "\n");
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
}
return sb.ToString();

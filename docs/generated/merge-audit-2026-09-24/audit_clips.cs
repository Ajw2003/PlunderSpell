// Lists every AnimationClip under Assets/Models/ArtBible and Assets/_Project: its host asset, length and curve count.
var sb = new System.Text.StringBuilder();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/_Project", "Assets/Models/ArtBible" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    foreach (var o in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
        if (o is AnimationClip c && !c.name.StartsWith("__preview__"))
            sb.AppendLine(path + " :: " + c.name + " len=" + c.length.ToString("0.00") + "s curves=" + UnityEditor.AnimationUtility.GetCurveBindings(c).Length);
}
return sb.ToString();

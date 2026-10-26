// Reimports the fear-pass art (#413) and reports what Unity made of it, for the import check.
// Run: bash Tools/Unity/eval.sh --file docs/generated/fear-import-413/fear_import_report.cs
var sb = new System.Text.StringBuilder();
string[] models =
{
    "Assets/Models/ArtBible/Enemies/Fear/TheWick/TheWick.fbx",
    "Assets/Models/ArtBible/Enemies/Fear/TheOphanOfHours/TheOphanOfHours.fbx",
    "Assets/Models/ArtBible/Animations/Humanoid_Fear.fbx",
    "Assets/Models/ArtBible/Animations/TheOphanOfHours_Generic.fbx",
};
UnityEditor.AssetDatabase.ImportAsset("Assets/Models/ArtBible/Enemies/Fear", UnityEditor.ImportAssetOptions.ForceUpdate | UnityEditor.ImportAssetOptions.ImportRecursive);
foreach (string path in models)
{
    UnityEditor.AssetDatabase.ImportAsset(path, UnityEditor.ImportAssetOptions.ForceUpdate);
    var importer = (UnityEditor.ModelImporter)UnityEditor.AssetImporter.GetAtPath(path);
    var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
    sb.Append(System.IO.Path.GetFileName(path)).Append(": rig=").Append(importer.animationType);
    var avatar = all.OfType<UnityEngine.Avatar>().FirstOrDefault();
    sb.Append(" avatar=").Append(avatar == null ? "none" : (avatar.isValid ? "valid" : "INVALID") + (avatar.isHuman ? "/human" : "/generic"));
    foreach (var clip in importer.clipAnimations)
    {
        sb.Append("\n   ").Append(clip.name).Append(" take=").Append(clip.takeName).Append(" loop=").Append(clip.loopTime);
        foreach (var e in clip.events)
            sb.Append(" [").Append(e.functionName).Append('@').Append(e.time.ToString("0.000")).Append(' ').Append(e.stringParameter).Append(']');
    }
    foreach (var m in all.OfType<UnityEngine.Material>())
        sb.Append("\n   material ").Append(m.name).Append(" emission=").Append(m.IsKeywordEnabled("_EMISSION"))
          .Append(" emissionMap=").Append(m.HasProperty("_EmissionMap") && m.GetTexture("_EmissionMap") != null);
    sb.Append('\n');
}
var mask = (UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath("Assets/Models/ArtBible/Enemies/Fear/TheWick/Textures/TheWick_FireMask.png");
sb.Append("TheWick_FireMask sRGB=").Append(mask.sRGBTexture);
return sb.ToString();

// Run in a live Editor with:  unity command eval_file --file <this file>
// Lists every era's enemies, loot and rooms as the trial merge wires them, and whether each
// enemy has anything to animate with. Read-only: loads assets, changes nothing.
var sb = new System.Text.StringBuilder();
var catGuids = UnityEditor.AssetDatabase.FindAssets("t:EraContentCatalogue");
sb.AppendLine("catalogues: " + catGuids.Length);
foreach (var g in catGuids)
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var cat = UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EraContentCatalogue>(path);
    sb.AppendLine("CATALOGUE " + path);
    foreach (RogueAi.Inventory.HistoricalEra era in System.Enum.GetValues(typeof(RogueAi.Inventory.HistoricalEra)))
    {
        var e = cat.For(era);
        if (e == null) { sb.AppendLine("  " + era + ": NO ENTRY"); continue; }
        sb.AppendLine("  " + era + ": rooms=" + (e.Rooms ? e.Rooms.name : "-") + " loot=" + (e.Loot ? e.Loot.name + "(" + e.Loot.Entries.Count + ")" : "-") + " enemies=" + (e.Enemies ? e.Enemies.name + "(" + e.Enemies.Entries.Count + ")" : "-"));
        if (e.Enemies)
            foreach (var en in e.Enemies.Entries)
            {
                var p = en.Prefab;
                string anim = "no prefab";
                if (p)
                {
                    var a = p.GetComponentInChildren<Animator>(true);
                    var smr = p.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length;
                    var mr = p.GetComponentsInChildren<MeshRenderer>(true).Length;
                    anim = "animator=" + (a ? (a.runtimeAnimatorController ? a.runtimeAnimatorController.name + "(" + a.runtimeAnimatorController.animationClips.Length + " clips)" : "no controller") : "none")
                         + " skinned=" + smr + " static=" + mr;
                    var src = p.GetComponentsInChildren<Transform>(true);
                    foreach (var t in src)
                    {
                        var root = UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(t.gameObject);
                        if (root && UnityEditor.AssetDatabase.GetAssetPath(root) != UnityEditor.AssetDatabase.GetAssetPath(p)) { anim += " model=" + UnityEditor.AssetDatabase.GetAssetPath(root); break; }
                    }
                }
                sb.AppendLine("    enemy " + en.EnemyId + " zone=" + en.Zone + " eraTag=" + en.Era + " anyEra=" + en.AnyEra + " " + anim);
            }
        if (e.Rooms)
        {
            var so = new UnityEditor.SerializedObject(e.Rooms);
            var it = so.GetIterator(); int n = 0; var names = new System.Collections.Generic.List<string>();
            while (it.Next(true))
                if (it.propertyType == UnityEditor.SerializedPropertyType.ObjectReference && it.objectReferenceValue is GameObject go) { n++; names.Add(go.name); }
            sb.AppendLine("    rooms (" + n + " prefab refs): " + string.Join(", ", names));
        }
    }
}
// Late Medieval models that exist on disk but no prefab points at yet (castle-bench's new rooms).
var lateModels = UnityEditor.AssetDatabase.FindAssets("t:Model", new[] { "Assets/_Project/Art/Models/Castle/LateMedieval" });
var latePrefabs = new System.Collections.Generic.HashSet<string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Castle/LateMedieval" }))
    latePrefabs.Add(System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GUIDToAssetPath(g)));
var orphan = new System.Collections.Generic.List<string>();
foreach (var g in lateModels)
{
    var n = System.IO.Path.GetFileNameWithoutExtension(UnityEditor.AssetDatabase.GUIDToAssetPath(g));
    if (!latePrefabs.Contains(n)) orphan.Add(n);
}
sb.AppendLine("LATE MEDIEVAL models=" + lateModels.Length + " prefabs=" + latePrefabs.Count + " models without prefab (" + orphan.Count + "): " + string.Join(", ", orphan));
// dreamy's AllEnemies prefabs: which enemies exist as art but are not in any roster.
var inRoster = new System.Collections.Generic.HashSet<string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:EnemyRoster"))
    foreach (var en in UnityEditor.AssetDatabase.LoadAssetAtPath<RogueAi.Raid.EnemyRoster>(UnityEditor.AssetDatabase.GUIDToAssetPath(g)).Entries)
        inRoster.Add(en.EnemyId);
var notRostered = new System.Collections.Generic.List<string>();
foreach (var g in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Models/ArtBible/AllEnemies" }))
{
    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(g);
    var n = System.IO.Path.GetFileNameWithoutExtension(path);
    var p = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    var a = p.GetComponentInChildren<Animator>(true);
    var clips = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.PrefabUtility.GetCorrespondingObjectFromOriginalSource(p) ?? p));
    sb.AppendLine("ALLENEMIES " + n + " rostered=" + inRoster.Contains(n) + " animator=" + (a ? (a.runtimeAnimatorController ? a.runtimeAnimatorController.name : "no controller") : "none") + " skinned=" + p.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
    if (!inRoster.Contains(n)) notRostered.Add(n);
}
sb.AppendLine("ArtBible enemies with no roster entry: " + string.Join(", ", notRostered));
var ctrls = UnityEditor.AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/_Project", "Assets/Models" });
var clipsAll = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/_Project", "Assets/Models/ArtBible" });
sb.AppendLine("AnimatorControllers under _Project/Models: " + ctrls.Length + "; AnimationClips: " + clipsAll.Length);
foreach (var g in ctrls) sb.AppendLine("  ctrl " + UnityEditor.AssetDatabase.GUIDToAssetPath(g));
return sb.ToString();

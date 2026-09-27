// #110: makes the Bronze Sword and Longsword melee weapons like the Arming Sword. Creates a
// MeleeWeaponStats asset for each (pointing at its InventoryItem), copies the Arming Sword's
// AcousticEmitter and MeleeWeapon onto its prefab, and points that MeleeWeapon at the new stats.
// Run once through `unity command eval_file` in Edit mode.
var log = new System.Text.StringBuilder();
var armingPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Weapons/ArmingSword.prefab");
var armingEmitter = armingPrefab.GetComponent<Plunderspell.Acoustics.AcousticEmitter>();
var armingMelee = armingPrefab.GetComponent<MeleeWeapon>();
var armingStats = UnityEditor.AssetDatabase.LoadAssetAtPath<MeleeWeaponStats>("Assets/_Project/Data/Inventory/ArmingSword_MeleeStats.asset");
var swords = new (string name, float reach)[] { ("BronzeSword", 1.4f), ("Longsword", 1.8f) };
foreach (var (name, reach) in swords)
{
    string statsPath = "Assets/_Project/Data/Inventory/" + name + "_MeleeStats.asset";
    var stats = UnityEditor.AssetDatabase.LoadAssetAtPath<MeleeWeaponStats>(statsPath);
    if (stats == null)
    {
        stats = UnityEngine.Object.Instantiate(armingStats);
        UnityEditor.AssetDatabase.CreateAsset(stats, statsPath);
    }
    var so = new UnityEditor.SerializedObject(stats);
    so.FindProperty("m_item").objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/Inventory/" + name + ".asset");
    so.FindProperty("m_reach").floatValue = reach;
    so.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.EditorUtility.SetDirty(stats);

    string prefabPath = "Assets/_Project/Prefabs/Weapons/" + name + ".prefab";
    var root = UnityEditor.PrefabUtility.LoadPrefabContents(prefabPath);
    if (root.GetComponent<Plunderspell.Acoustics.AcousticEmitter>() == null)
    {
        UnityEditorInternal.ComponentUtility.CopyComponent(armingEmitter);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(root);
    }
    var melee = root.GetComponent<MeleeWeapon>();
    if (melee == null)
    {
        UnityEditorInternal.ComponentUtility.CopyComponent(armingMelee);
        UnityEditorInternal.ComponentUtility.PasteComponentAsNew(root);
        melee = root.GetComponent<MeleeWeapon>();
    }
    var mso = new UnityEditor.SerializedObject(melee);
    mso.FindProperty("m_stats").objectReferenceValue = stats;
    mso.ApplyModifiedPropertiesWithoutUndo();
    UnityEditor.PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    log.Append(name + ": weight " + stats.Weight + ", damage " + stats.Damage + ", swing " + stats.SwingDuration.ToString("F2") + " s, reach " + stats.Reach + "; ");
}
UnityEditor.EditorUtility.SetDirty(armingStats);
UnityEditor.AssetDatabase.SaveAssets();
log.Append("ArmingSword: damage " + armingStats.Damage + ", swing " + armingStats.SwingDuration.ToString("F2") + " s");
return log.ToString();
